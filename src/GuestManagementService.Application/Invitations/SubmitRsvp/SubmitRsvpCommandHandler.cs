using FluentValidation;
using FluentValidation.Results;
using GuestManagementService.Application.Abstractions.Common;
using GuestManagementService.Application.Abstractions.EventReferences;
using GuestManagementService.Application.Abstractions.Guests;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GuestManagementService.Application.Invitations.SubmitRsvp;

/// <summary>
/// Records a guest's own RSVP.
/// </summary>
/// <remarks>
/// The only write path in the system reachable without authenticating, so the guardrails matter
/// more than the logic:
/// <list type="bullet">
/// <item>The guest's answer can never raise the organiser's <c>plusOnes</c> allowance — the two
/// live in separate metadata fields and only the guest's is written here.</item>
/// <item>The deadline governs edits as well as first submissions; an organiser who has confirmed
/// catering numbers should not see them move afterwards.</item>
/// <item>Every not-found path returns the same result, so the endpoint cannot be used to learn
/// which tokens are real.</item>
/// </list>
/// </remarks>
public sealed class SubmitRsvpCommandHandler(
    IGuestRepository guestRepository,
    IEventReferenceRepository eventReferenceRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<SubmitRsvpCommandHandler> logger)
    : IRequestHandler<SubmitRsvpCommand, SubmitRsvpResult>
{
    public async Task<SubmitRsvpResult> Handle(SubmitRsvpCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
        {
            return SubmitRsvpResult.NotFound();
        }

        var guest = await guestRepository.GetByInvitationTokenAsync(request.Token, cancellationToken);

        if (guest is null)
        {
            // Never logs the token — it is credential-like.
            logger.LogInformation("RSVP submitted against an unknown token.");

            return SubmitRsvpResult.NotFound();
        }

        var eventReference = await eventReferenceRepository.GetByIdAsync(guest.EventId, cancellationToken);

        if (eventReference is null || eventReference.IsDeleted)
        {
            return SubmitRsvpResult.NotFound();
        }

        var now = timeProvider.GetUtcNow();

        if (!RsvpDeadline.IsOpen(eventReference.EventDate, eventReference.TimeZoneId, now))
        {
            logger.LogInformation(
                "RSVP rejected after the deadline. GuestId: {GuestId}.",
                guest.Id);

            return SubmitRsvpResult.Closed();
        }

        var allowance = InvitationMetadata.ReadPlusOnesAllowed(guest.Metadata);
        var confirmed = request.PlusOnesConfirmed ?? 0;

        if (confirmed > allowance)
        {
            // Checked here rather than in the validator because it depends on the guest's own
            // record. Without it, an anonymous caller could book more seats than they were offered.
            throw new ValidationException(new[]
            {
                new ValidationFailure(
                    nameof(SubmitRsvpCommand.PlusOnesConfirmed),
                    allowance == 0
                        ? "Your invitation does not include additional guests."
                        : $"You're invited with up to {allowance} guests. Lower this to continue."),
            });
        }

        SubmitRsvpCommandValidator.TryParse(request.RsvpStatus, out var status);

        guest.RecordRsvp(
            status,
            InvitationMetadata.WriteGuestAnswers(guest.Metadata, confirmed, request.DietaryNotes),
            now);

        await guestRepository.UpdateAsync(guest, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "RSVP recorded. GuestId: {GuestId}. Status: {RsvpStatus}.",
            guest.Id,
            status);

        return SubmitRsvpResult.Accepted(
            guest,
            eventReference,
            RsvpDeadline.Compute(eventReference.EventDate, eventReference.TimeZoneId));
    }
}
