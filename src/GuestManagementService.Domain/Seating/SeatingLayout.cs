namespace GuestManagementService.Domain.Seating;

// Aggregate root for an event's seating arrangement (one per event, tenant-scoped).
// Owns the tables placed in the room; seat assignments and floor-plan areas are
// added in later slices.
public sealed class SeatingLayout
{
    private readonly List<SeatingTable> _tables = [];
    private readonly List<SeatAssignment> _assignments = [];
    private readonly List<FloorPlanArea> _areas = [];

    private SeatingLayout()
    {
    }

    private SeatingLayout(Guid id, Guid eventId, Guid tenantId, DateTimeOffset createdAt)
    {
        Id = id;
        EventId = eventId;
        TenantId = tenantId;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid EventId { get; private set; }

    public Guid TenantId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public IReadOnlyCollection<SeatingTable> Tables => _tables.AsReadOnly();

    public IReadOnlyCollection<SeatAssignment> Assignments => _assignments.AsReadOnly();

    public IReadOnlyCollection<FloorPlanArea> Areas => _areas.AsReadOnly();

    public static SeatingLayout Create(Guid id, Guid eventId, Guid tenantId, DateTimeOffset createdAt)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Seating layout id must not be empty.", nameof(id));
        }

        if (eventId == Guid.Empty)
        {
            throw new ArgumentException("Event id must not be empty.", nameof(eventId));
        }

        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("Tenant id must not be empty.", nameof(tenantId));
        }

        return new SeatingLayout(id, eventId, tenantId, createdAt.ToUniversalTime());
    }

    public SeatingTable AddTable(
        Guid tableId,
        string name,
        TableShape shape,
        int seatCount,
        DateTimeOffset now)
    {
        var table = SeatingTable.Create(tableId, Id, name, shape, seatCount, now);
        _tables.Add(table);
        UpdatedAt = now.ToUniversalTime();
        return table;
    }

    public SeatingTable? FindTable(Guid tableId)
    {
        return _tables.FirstOrDefault(table => table.Id == tableId);
    }

    public bool RemoveTable(Guid tableId, DateTimeOffset now)
    {
        var table = FindTable(tableId);
        if (table is null)
        {
            return false;
        }

        _tables.Remove(table);
        _assignments.RemoveAll(assignment => assignment.SeatingTableId == tableId);
        UpdatedAt = now.ToUniversalTime();
        return true;
    }

    // A guest occupies at most one seat directly in the layout, plus whatever seats are
    // reserved for their party (see AssignGuestWithParty): assigning them elsewhere (or
    // re-assigning the same seat) first drops every prior assignment tied to that guest
    // — their own seat and any party reservations — so this call is idempotent and
    // doubles as "move". A thin wrapper over AssignGuestWithParty with zero accompanying
    // guests, so there's one implementation of the occupancy/move rules to keep correct —
    // this can never silently strand a party's reserved seats the way a separate
    // single-seat implementation could.
    public SeatAssignmentOutcome AssignGuest(
        Guid tableId,
        int seatIndex,
        Guid guestId,
        DateTimeOffset now,
        out SeatAssignment? assignment)
    {
        var outcome = AssignGuestWithParty(tableId, seatIndex, guestId, 0, now, out var assignments);
        assignment = assignments.Count > 0 ? assignments[0] : null;
        return outcome;
    }

    // Assigns guestId to seatIndex and, when accompanyingGuestCount > 0, reserves that many
    // additional contiguous adjacent seats for their party (see SeatAdjacency) — anonymous
    // seats with no guest record of their own. All-or-nothing: if there isn't enough
    // contiguous room, nothing changes and InsufficientAdjacentSeats is returned. Like
    // AssignGuest, this drops every prior assignment tied to the guest first, so it also
    // doubles as "move the whole party".
    public SeatAssignmentOutcome AssignGuestWithParty(
        Guid tableId,
        int seatIndex,
        Guid guestId,
        int accompanyingGuestCount,
        DateTimeOffset now,
        out IReadOnlyList<SeatAssignment> assignments)
    {
        if (accompanyingGuestCount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(accompanyingGuestCount),
                accompanyingGuestCount,
                "Accompanying guest count must not be negative.");
        }

        var table = FindTable(tableId)
            ?? throw new InvalidOperationException($"Table {tableId} does not belong to this layout.");

        ValidateSeatIndex(table, seatIndex);

        // Seats already held by this guest's own party aren't "occupied" for this search —
        // they'll be released and re-claimed as part of the move.
        var occupiedSeatIndexes = _assignments
            .Where(a => a.SeatingTableId == tableId && a.PartyOwnerGuestId != guestId)
            .Select(a => a.SeatIndex)
            .ToHashSet();

        if (occupiedSeatIndexes.Contains(seatIndex))
        {
            assignments = [];
            return SeatAssignmentOutcome.SeatOccupied;
        }

        var requiredSeatCount = 1 + accompanyingGuestCount;
        var chosenSeatIndexes = SeatAdjacency.FindContiguousFreeSeats(
            table.Shape, table.SeatCount, seatIndex, requiredSeatCount, occupiedSeatIndexes);

        if (chosenSeatIndexes is null)
        {
            assignments = [];
            return SeatAssignmentOutcome.InsufficientAdjacentSeats;
        }

        _assignments.RemoveAll(a => a.PartyOwnerGuestId == guestId);

        var created = new List<SeatAssignment>(chosenSeatIndexes.Count)
        {
            SeatAssignment.Create(Guid.NewGuid(), tableId, guestId, chosenSeatIndexes[0], now),
        };
        created.AddRange(chosenSeatIndexes.Skip(1)
            .Select(reservedSeatIndex => SeatAssignment.CreateReservedForParty(Guid.NewGuid(), tableId, guestId, reservedSeatIndex, now)));

        _assignments.AddRange(created);
        UpdatedAt = now.ToUniversalTime();
        assignments = created;
        return SeatAssignmentOutcome.Assigned;
    }

    private static void ValidateSeatIndex(SeatingTable table, int seatIndex)
    {
        if (seatIndex < 0 || seatIndex >= table.SeatCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(seatIndex),
                seatIndex,
                $"Seat index must be between 0 and {table.SeatCount - 1} for this table.");
        }
    }

    // Releases the seat and, when it belongs to a party (a primary seat with reservations,
    // or a reserved seat itself), every other seat held by the same party — so unseating
    // one member of a group doesn't leave their reserved seats stranded.
    public bool UnassignSeat(Guid tableId, int seatIndex, DateTimeOffset now)
    {
        var assignment = _assignments.FirstOrDefault(a => a.SeatingTableId == tableId && a.SeatIndex == seatIndex);
        if (assignment is null)
        {
            return false;
        }

        return UnassignParty(assignment.PartyOwnerGuestId, now);
    }

    // Releases every seat held by guestId's party (their own seat plus any reserved for
    // their accompanying attendees).
    public bool UnassignGuest(Guid guestId, DateTimeOffset now)
    {
        return UnassignParty(guestId, now);
    }

    private bool UnassignParty(Guid partyOwnerGuestId, DateTimeOffset now)
    {
        var removed = _assignments.RemoveAll(a => a.PartyOwnerGuestId == partyOwnerGuestId);
        if (removed == 0)
        {
            return false;
        }

        UpdatedAt = now.ToUniversalTime();
        return true;
    }

    public FloorPlanArea AddArea(
        Guid areaId,
        string name,
        AreaKind kind,
        AreaShape shape,
        double width,
        double height,
        string? color,
        int? capacity,
        DateTimeOffset now)
    {
        var area = FloorPlanArea.Create(areaId, Id, name, kind, shape, width, height, color, capacity, now);
        _areas.Add(area);
        UpdatedAt = now.ToUniversalTime();
        return area;
    }

    public FloorPlanArea? FindArea(Guid areaId)
    {
        return _areas.FirstOrDefault(area => area.Id == areaId);
    }

    public bool RemoveArea(Guid areaId, DateTimeOffset now)
    {
        var area = FindArea(areaId);
        if (area is null)
        {
            return false;
        }

        _areas.Remove(area);
        UpdatedAt = now.ToUniversalTime();
        return true;
    }
}
