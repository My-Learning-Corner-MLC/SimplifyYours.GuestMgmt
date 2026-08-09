using MediatR;

namespace GuestManagementService.Application.EventReferences.ApplyEventReferenceEvent;

public sealed record ApplyEventReferenceEventCommand(
    Guid MessageId,
    string EventType,
    Guid EventId,
    string EventName,
    DateTimeOffset OccurredAt,
    string PlannedEventType,
    Guid TenantId = default,
    int PayloadVersion = 0,
    DateOnly? EventDate = null,
    TimeOnly? EventStartTime = null,
    TimeOnly? EventEndTime = null,
    string? TimeZoneId = null,
    string? EventDescription = null,
    string? VenueName = null,
    string? VenueAddress = null,
    string? VenueNotes = null)
    : IRequest<bool>;
