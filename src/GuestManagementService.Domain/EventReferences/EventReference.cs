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

    public DateOnly? EventDate { get; private set; }

    public TimeOnly? EventStartTime { get; private set; }

    public string? TimeZoneId { get; private set; }

    public string? EventDescription { get; private set; }

    public string? VenueName { get; private set; }

    public string? VenueAddress { get; private set; }

    public string? VenueNotes { get; private set; }

    public static EventReference Active(
        Guid eventId,
        string eventName,
        Guid tenantId,
        DateTimeOffset syncedAt,
        string eventType,
        DateOnly? eventDate = null,
        TimeOnly? eventStartTime = null,
        string? timeZoneId = null,
        string? eventDescription = null,
        string? venueName = null,
        string? venueAddress = null,
        string? venueNotes = null)
    {
        if (eventId == Guid.Empty)
        {
            throw new ArgumentException("Event id must not be empty.", nameof(eventId));
        }

        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("Tenant id must not be empty.", nameof(tenantId));
        }

        var reference = new EventReference(eventId, eventName, tenantId, isDeleted: false, syncedAt, eventType);
        reference.SetDisplayFields(
            eventDate,
            eventStartTime,
            timeZoneId,
            eventDescription,
            venueName,
            venueAddress,
            venueNotes);
        return reference;
    }

    public void MarkActive(
        string eventName,
        Guid tenantId,
        DateTimeOffset syncedAt,
        string eventType,
        DateOnly? eventDate = null,
        TimeOnly? eventStartTime = null,
        string? timeZoneId = null,
        string? eventDescription = null,
        string? venueName = null,
        string? venueAddress = null,
        string? venueNotes = null)
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
        SetDisplayFields(
            eventDate,
            eventStartTime,
            timeZoneId,
            eventDescription,
            venueName,
            venueAddress,
            venueNotes);
    }

    public void MarkDeleted(DateTimeOffset syncedAt)
    {
        IsDeleted = true;
        LastSyncedAt = syncedAt.ToUniversalTime();
    }

    private void SetDisplayFields(
        DateOnly? eventDate,
        TimeOnly? eventStartTime,
        string? timeZoneId,
        string? eventDescription,
        string? venueName,
        string? venueAddress,
        string? venueNotes)
    {
        EventDate = eventDate;
        EventStartTime = eventStartTime;
        TimeZoneId = NormalizeOptionalText(timeZoneId);
        EventDescription = NormalizeOptionalText(eventDescription);
        VenueName = NormalizeOptionalText(venueName);
        VenueAddress = NormalizeOptionalText(venueAddress);
        VenueNotes = NormalizeOptionalText(venueNotes);
    }

    private static string NormalizeEventName(string eventName)
    {
        if (string.IsNullOrWhiteSpace(eventName))
        {
            throw new ArgumentException("Event name is required.", nameof(eventName));
        }

        return eventName.Trim();
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
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
