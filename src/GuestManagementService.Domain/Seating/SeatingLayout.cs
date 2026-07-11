namespace GuestManagementService.Domain.Seating;

// Aggregate root for an event's seating arrangement (one per event, tenant-scoped).
// Owns the tables placed in the room; seat assignments and floor-plan areas are
// added in later slices.
public sealed class SeatingLayout
{
    private readonly List<SeatingTable> _tables = [];

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
}
