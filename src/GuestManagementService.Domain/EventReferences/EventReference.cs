namespace GuestManagementService.Domain.EventReferences;

public sealed class EventReference
{
    private EventReference()
    {
    }

    private EventReference(Guid eventId, string eventName, bool isDeleted, DateTimeOffset lastSyncedAt)
    {
        EventId = eventId;
        EventName = NormalizeEventName(eventName);
        IsDeleted = isDeleted;
        LastSyncedAt = lastSyncedAt.ToUniversalTime();
    }

    public Guid EventId { get; private set; }

    public string EventName { get; private set; } = string.Empty;

    public bool IsDeleted { get; private set; }

    public DateTimeOffset LastSyncedAt { get; private set; }

    public static EventReference Active(Guid eventId, string eventName, DateTimeOffset syncedAt)
    {
        if (eventId == Guid.Empty)
        {
            throw new ArgumentException("Event id must not be empty.", nameof(eventId));
        }

        return new EventReference(eventId, eventName, isDeleted: false, syncedAt);
    }

    public void MarkActive(string eventName, DateTimeOffset syncedAt)
    {
        EventName = NormalizeEventName(eventName);
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
