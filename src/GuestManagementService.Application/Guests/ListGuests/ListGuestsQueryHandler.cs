using GuestManagementService.Application.Abstractions.EventReferences;
using GuestManagementService.Application.Abstractions.Guests;
using GuestManagementService.Application.Guests;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GuestManagementService.Application.Guests.ListGuests;

public sealed class ListGuestsQueryHandler(
    IEventReferenceRepository eventReferenceRepository,
    IGuestRepository guestRepository,
    ILogger<ListGuestsQueryHandler> logger)
    : IRequestHandler<ListGuestsQuery, ListGuestsResult>
{
    public async Task<ListGuestsResult> Handle(ListGuestsQuery request, CancellationToken cancellationToken)
    {
        var currentUser = request.CurrentUser;

        var eventReference = await eventReferenceRepository.GetByIdAsync(request.EventId, cancellationToken);

        if (eventReference is null || eventReference.IsDeleted)
        {
            logger.LogWarning(
                "Guest list requested but event reference was not available. EventId: {EventId}.",
                request.EventId);
            return ListGuestsResult.EventNotFound();
        }

        if (eventReference.TenantId != currentUser.TenantId)
        {
            logger.LogWarning(
                "Guest list requested but event reference is owned by another tenant. EventId: {EventId}.",
                request.EventId);
            return ListGuestsResult.EventNotFound();
        }

        var guests = await guestRepository.ListByEventAsync(request.EventId, cancellationToken);

        logger.LogInformation(
            "Guest list returned {GuestCount} guests for event {EventId}.",
            guests.Count,
            request.EventId);

        return ListGuestsResult.Found(guests.Select(GuestDetails.From).ToList());
    }
}
