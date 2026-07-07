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
        DateTimeOffset lastSyncedAt)
    {
        EventId = eventId;
        EventName = NormalizeEventName(eventName);
        TenantId = tenantId;
        IsDeleted = isDeleted;
        LastSyncedAt = lastSyncedAt.ToUniversalTime();
    }

    public Guid EventId { get; private set; }

    public string EventName { get; private set; } = string.Empty;

    public Guid TenantId { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTimeOffset LastSyncedAt { get; private set; }

    public static EventReference Active(
        Guid eventId,
        string eventName,
        Guid tenantId,
        DateTimeOffset syncedAt)
    {
        if (eventId == Guid.Empty)
        {
            throw new ArgumentException("Event id must not be empty.", nameof(eventId));
        }

        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("Tenant id must not be empty.", nameof(tenantId));
        }

        return new EventReference(eventId, eventName, tenantId, isDeleted: false, syncedAt);
    }

    public void MarkActive(string eventName, Guid tenantId, DateTimeOffset syncedAt)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("Tenant id must not be empty.", nameof(tenantId));
        }

        EventName = NormalizeEventName(eventName);
        TenantId = tenantId;
        IsDeleted = false;
        LastSyncedAt = syncedAt.ToUniversalTime();
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
}
