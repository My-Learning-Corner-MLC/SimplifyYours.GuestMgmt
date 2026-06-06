using GuestManagementService.Application.Abstractions.Common;
using GuestManagementService.Application.Abstractions.EventReferences;
using GuestManagementService.Domain.EventReferences;
using MediatR;

namespace GuestManagementService.Application.EventReferences.ApplyEventReferenceEvent;

public sealed class ApplyEventReferenceEventCommandHandler(
    IEventReferenceRepository eventReferenceRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ApplyEventReferenceEventCommand, bool>
{
    public async Task<bool> Handle(
        ApplyEventReferenceEventCommand request,
        CancellationToken cancellationToken)
    {
        var eventReference = await eventReferenceRepository.GetByIdAsync(request.EventId, cancellationToken);

        if (eventReference is null)
        {
            eventReference = EventReference.Active(request.EventId, request.EventName, request.OccurredAt);
        }

        if (request.EventType.Equals("EventDeleted", StringComparison.OrdinalIgnoreCase))
        {
            eventReference.MarkDeleted(request.OccurredAt);
        }
        else
        {
            eventReference.MarkActive(request.EventName, request.OccurredAt);
        }

        await eventReferenceRepository.UpsertAsync(eventReference, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
