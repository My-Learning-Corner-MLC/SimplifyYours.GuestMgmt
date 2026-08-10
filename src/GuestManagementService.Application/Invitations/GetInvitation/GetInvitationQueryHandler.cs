using GuestManagementService.Application.Abstractions.EventReferences;
using GuestManagementService.Application.Abstractions.Guests;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GuestManagementService.Application.Invitations.GetInvitation;

/// <summary>
/// Resolves an invitation token to the data the public page renders.
/// </summary>
/// <remarks>
/// Every failure returns the same NotFound: an unknown token, a malformed one, and a token whose
/// event has been deleted are indistinguishable to the caller. Anything finer would let someone
/// probe which tokens are real.
/// </remarks>
public sealed class GetInvitationQueryHandler(
    IGuestRepository guestRepository,
    IEventReferenceRepository eventReferenceRepository,
    TimeProvider timeProvider,
    ILogger<GetInvitationQueryHandler> logger)
    : IRequestHandler<GetInvitationQuery, GetInvitationResult>
{
    public async Task<GetInvitationResult> Handle(
        GetInvitationQuery request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
        {
            return GetInvitationResult.NotFound();
        }

        var guest = await guestRepository.GetByInvitationTokenAsync(request.Token, cancellationToken);

        if (guest is null)
        {
            // Deliberately logs the token's absence, never the token itself — it is credential-like.
            logger.LogInformation("Invitation requested for an unknown token.");

            return GetInvitationResult.NotFound();
        }

        var eventReference = await eventReferenceRepository.GetByIdAsync(guest.EventId, cancellationToken);

        if (eventReference is null || eventReference.IsDeleted)
        {
            // A cancelled event must not keep rendering an invitation to it.
            logger.LogInformation(
                "Invitation resolved but its event is unavailable. EventId: {EventId}.",
                guest.EventId);

            return GetInvitationResult.NotFound();
        }

        var deadline = RsvpDeadline.Compute(eventReference.EventDate, eventReference.TimeZoneId);
        var isOpen = RsvpDeadline.IsOpen(
            eventReference.EventDate,
            eventReference.TimeZoneId,
            timeProvider.GetUtcNow());

        return GetInvitationResult.Found(guest, eventReference, deadline, isOpen);
    }
}
