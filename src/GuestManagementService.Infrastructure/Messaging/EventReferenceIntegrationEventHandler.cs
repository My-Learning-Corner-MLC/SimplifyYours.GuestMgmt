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
                context.Envelope.OccurredAt,
                context.Payload.EventType,
                context.Payload.TenantId,
                context.Envelope.Version,
                context.Payload.EventDate,
                context.Payload.EventStartTime,
                context.Payload.EventEndTime,
                context.Payload.TimeZoneId,
                context.Payload.EventDescription,
                context.Payload.Location?.VenueName,
                context.Payload.Location?.Address,
                context.Payload.Location?.Notes),
            cancellationToken);
    }
}
