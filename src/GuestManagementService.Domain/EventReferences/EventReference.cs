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

    public TimeOnly? EventEndTime { get; private set; }

    public string? TimeZoneId { get; private set; }

    public string? EventDescription { get; private set; }

    public string? VenueName { get; private set; }

    public string? VenueAddress { get; private set; }

    public string? VenueNotes { get; private set; }

    /// <summary>
    /// Replaces the display fields rendered on public invitation pages. The producer is the source
    /// of truth and always sends its full current state, so a null here means "cleared", not
    /// "unknown". Callers must therefore only invoke this for payloads that actually carry the
    /// fields (see <c>EventReferencePayload.DisplayFieldsVersion</c>).
    /// </summary>
    /// <remarks>
    /// Deliberately does not throw on over-long text: event-service validates lengths at write
    /// time, and throwing inside a Kafka consumer would poison the partition by failing the same
    /// message forever. Values are trimmed and blanks collapsed to null.
    /// </remarks>
    public void ApplyDisplayDetails(
        DateOnly? eventDate,
        TimeOnly? eventStartTime,
        TimeOnly? eventEndTime,
        string? timeZoneId,
        string? eventDescription,
        string? venueName,
        string? venueAddress,
        string? venueNotes)
    {
        EventDate = eventDate;
        EventStartTime = eventStartTime;
        EventEndTime = eventEndTime;
        TimeZoneId = NormalizeOptionalText(timeZoneId);
        EventDescription = NormalizeOptionalText(eventDescription);
        VenueName = NormalizeOptionalText(venueName);
        VenueAddress = NormalizeOptionalText(venueAddress);
        VenueNotes = NormalizeOptionalText(venueNotes);
    }

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
