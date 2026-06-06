using GuestManagementService.Application.EventReferences.ApplyEventReferenceEvent;
using GuestManagementService.Contracts.IntegrationEvents;
using MediatR;
using SimplifyYours.Event.Abstractions;

namespace GuestManagementService.Infrastructure.Messaging;

internal sealed class EventReferenceIntegrationEventHandler(ISender sender)
    : IIntegrationEventHandler<EventReferencePayload>
{
    public async Task HandleAsync(
        IntegrationEventContext<EventReferencePayload> context,
        CancellationToken cancellationToken)
    {
        await sender.Send(
            new ApplyEventReferenceEventCommand(
                context.Envelope.EventId,
                context.Envelope.EventType,
                context.Payload.EventId,
                context.Payload.EventName,
                context.Envelope.OccurredAt),
            cancellationToken);
    }
}
