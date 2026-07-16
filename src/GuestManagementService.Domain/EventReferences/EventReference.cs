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
        string eventType)
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

    public string EventType { get; private set; } = string.Empty;

    public static EventReference Active(
        Guid eventId,
        string eventName,
        Guid tenantId,
        DateTimeOffset syncedAt,
        string eventType)
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

    public void MarkActive(string eventName, Guid tenantId, DateTimeOffset syncedAt, string eventType)
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

    private static string NormalizeEventType(string eventType)
    {
        if (string.IsNullOrWhiteSpace(eventType))
        {
            throw new ArgumentException("Event type is required.", nameof(eventType));
        }

        return eventType.Trim().ToLowerInvariant();
    }
}
