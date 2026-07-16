namespace GuestManagementService.Domain.EventReferences;

public sealed class EventReference
{
    private EventReference()
    {
    }

    private EventReference(
        Guid eventId,
        string eventName,
        Guid tenantId,
        bool isDeleted,
        DateTimeOffset lastSyncedAt,
        string? eventType)
    {
        EventId = eventId;
        EventName = NormalizeEventName(eventName);
        TenantId = tenantId;
        IsDeleted = isDeleted;
        LastSyncedAt = lastSyncedAt.ToUniversalTime();
        EventType = NormalizeEventType(eventType);
    }

    public Guid EventId { get; private set; }

    public string EventName { get; private set; } = string.Empty;

    public Guid TenantId { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTimeOffset LastSyncedAt { get; private set; }

    // The event's business type (wedding, birthday, ...), synced from event-service. Null for
    // references synced before this field existed, until the next EventCreated/EventUpdated
    // message backfills it. Guest metadata mapping is resolved from this value — see
    // Application/Guests/IGuestMetadataMapper.
    public string? EventType { get; private set; }

    public static EventReference Active(
        Guid eventId,
        string eventName,
        Guid tenantId,
        DateTimeOffset syncedAt,
        string? eventType = null)
    {
        if (eventId == Guid.Empty)
        {
            throw new ArgumentException("Event id must not be empty.", nameof(eventId));
        }

        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("Tenant id must not be empty.", nameof(tenantId));
        }

        return new EventReference(eventId, eventName, tenantId, isDeleted: false, syncedAt, eventType);
    }

    public void MarkActive(string eventName, Guid tenantId, DateTimeOffset syncedAt, string? eventType = null)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("Tenant id must not be empty.", nameof(tenantId));
        }

        EventName = NormalizeEventName(eventName);
        TenantId = tenantId;
        IsDeleted = false;
        LastSyncedAt = syncedAt.ToUniversalTime();
        EventType = NormalizeEventType(eventType);
    }

    public void MarkDeleted(DateTimeOffset syncedAt)
    {
        IsDeleted = true;
        LastSyncedAt = syncedAt.ToUniversalTime();
    }

    private static string NormalizeEventName(string eventName)
    {
        if (string.IsNullOrWhiteSpace(eventName))
        {
            throw new ArgumentException("Event name is required.", nameof(eventName));
        }

        return eventName.Trim();
    }

    private static string? NormalizeEventType(string? eventType)
    {
        return string.IsNullOrWhiteSpace(eventType) ? null : eventType.Trim().ToLowerInvariant();
    }
}
