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

    // A guest occupies at most one seat in the layout: assigning them elsewhere
    // (or re-assigning the same seat) first drops any prior assignment for that
    // guest, so this call is idempotent and doubles as "move".
    public SeatAssignmentOutcome AssignGuest(
        Guid tableId,
        int seatIndex,
        Guid guestId,
        DateTimeOffset now,
        out SeatAssignment? assignment)
    {
        var table = FindTable(tableId)
            ?? throw new InvalidOperationException($"Table {tableId} does not belong to this layout.");

        if (seatIndex < 0 || seatIndex >= table.SeatCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(seatIndex),
                seatIndex,
                $"Seat index must be between 0 and {table.SeatCount - 1} for this table.");
        }

        var occupant = _assignments.FirstOrDefault(a => a.SeatingTableId == tableId && a.SeatIndex == seatIndex);
        if (occupant is not null && occupant.GuestId != guestId)
        {
            assignment = null;
            return SeatAssignmentOutcome.SeatOccupied;
        }

        _assignments.RemoveAll(a => a.GuestId == guestId);

        assignment = SeatAssignment.Create(Guid.NewGuid(), tableId, guestId, seatIndex, now);
        _assignments.Add(assignment);
        UpdatedAt = now.ToUniversalTime();
        return SeatAssignmentOutcome.Assigned;
    }

    public bool UnassignSeat(Guid tableId, int seatIndex, DateTimeOffset now)
    {
        var assignment = _assignments.FirstOrDefault(a => a.SeatingTableId == tableId && a.SeatIndex == seatIndex);
        if (assignment is null)
        {
            return false;
        }

        _assignments.Remove(assignment);
        UpdatedAt = now.ToUniversalTime();
        return true;
    }

    public bool UnassignGuest(Guid guestId, DateTimeOffset now)
    {
        var assignment = _assignments.FirstOrDefault(a => a.GuestId == guestId);
        if (assignment is null)
        {
            return false;
        }

        _assignments.Remove(assignment);
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
