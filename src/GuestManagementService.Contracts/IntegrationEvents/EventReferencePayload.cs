namespace GuestManagementService.Contracts.IntegrationEvents;

public sealed record EventReferencePayload(
    Guid EventId,
    string EventName,
    Guid TenantId,
    string EventType,
    DateOnly? EventDate = null,
    TimeOnly? EventStartTime = null,
    TimeOnly? EventEndTime = null,
    string? TimeZoneId = null,
    string? EventDescription = null,
    EventReferenceLocationPayload? Location = null)
{
    /// <summary>
    /// First envelope version in which event-service publishes the display fields
    /// (date, times, time zone, description, location). Messages below this version carry
    /// none of them, so their absence must not be mistaken for "cleared" — see
    /// <c>ApplyEventReferenceEventCommandHandler</c>.
    /// </summary>
    public const int DisplayFieldsVersion = 4;
}

public sealed record EventReferenceLocationPayload(
    string? VenueName,
    string? Address,
    string? Notes);
