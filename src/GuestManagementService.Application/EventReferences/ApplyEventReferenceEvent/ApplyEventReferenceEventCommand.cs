using MediatR;

namespace GuestManagementService.Application.EventReferences.ApplyEventReferenceEvent;

public sealed record ApplyEventReferenceEventCommand(
    Guid MessageId,
    string EventType,
    Guid EventId,
    string EventName,
    DateTimeOffset OccurredAt,
    Guid TenantId = default)
    : IRequest<bool>;
