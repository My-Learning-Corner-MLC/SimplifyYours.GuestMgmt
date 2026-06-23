using GuestManagementService.Application.Abstractions.Common;
using GuestManagementService.Application.Abstractions.EventReferences;
using GuestManagementService.Domain.EventReferences;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GuestManagementService.Application.EventReferences.ApplyEventReferenceEvent;

public sealed class ApplyEventReferenceEventCommandHandler(
    IEventReferenceRepository eventReferenceRepository,
    IUnitOfWork unitOfWork,
    ILogger<ApplyEventReferenceEventCommandHandler> logger)
    : IRequestHandler<ApplyEventReferenceEventCommand, bool>
{
    public async Task<bool> Handle(
        ApplyEventReferenceEventCommand request,
        CancellationToken cancellationToken)
    {
        var eventReference = await eventReferenceRepository.GetByIdAsync(request.EventId, cancellationToken);

        if (eventReference is null)
        {
            eventReference = EventReference.Active(
                request.EventId,
                request.EventName,
                request.TenantId,
                request.OccurredAt);
            logger.LogInformation(
                "Event reference created from integration event. MessageId: {MessageId}. EventId: {EventId}. EventType: {EventType}.",
                request.MessageId,
                request.EventId,
                request.EventType);
        }

        if (request.EventType.Equals("EventDeleted", StringComparison.OrdinalIgnoreCase))
        {
            eventReference.MarkDeleted(request.OccurredAt);
            logger.LogInformation(
                "Event reference marked deleted from integration event. MessageId: {MessageId}. EventId: {EventId}.",
                request.MessageId,
                request.EventId);
        }
        else
        {
            eventReference.MarkActive(request.EventName, request.TenantId, request.OccurredAt);
            logger.LogInformation(
                "Event reference marked active from integration event. MessageId: {MessageId}. EventId: {EventId}. EventType: {EventType}.",
                request.MessageId,
                request.EventId,
                request.EventType);
        }

        await eventReferenceRepository.UpsertAsync(eventReference, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
