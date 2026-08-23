using FluentValidation;
using FluentValidation.Results;
using GuestManagementService.Application.Abstractions.Common;
using GuestManagementService.Application.Abstractions.EventReferences;
using GuestManagementService.Application.Abstractions.Guests;
using GuestManagementService.Application.Abstractions.Invitations;
using MediatR;

namespace GuestManagementService.Application.Invitations.RotatePublicToken;

/// <summary>Owner-scoped, same reasoning as the other invitation-settings handlers.</summary>
public sealed class RotatePublicTokenCommandHandler(
    IEventReferenceRepository eventReferenceRepository,
    IEventInvitationSettingsRepository settingsRepository,
    IInvitationTokenGenerator tokenGenerator,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : IRequestHandler<RotatePublicTokenCommand, RotatePublicTokenResult>
{
    public async Task<RotatePublicTokenResult> Handle(
        RotatePublicTokenCommand request,
        CancellationToken cancellationToken)
    {
        var eventReference = await eventReferenceRepository.GetByIdAsync(request.EventId, cancellationToken);

        if (eventReference is null
            || eventReference.IsDeleted
            || eventReference.TenantId != request.CurrentUser.TenantId)
        {
            return RotatePublicTokenResult.EventNotFound();
        }

        var settings = await settingsRepository.GetByEventIdAsync(
            request.EventId,
            request.CurrentUser.TenantId,
            cancellationToken);

        if (settings is null || !settings.PublicLinkEnabled || string.IsNullOrEmpty(settings.PublicEventToken))
        {
            throw new ValidationException(
            [
                new ValidationFailure(
                    nameof(RotatePublicTokenCommand.EventId),
                    "The public link has not been enabled yet."),
            ]);
        }

        var now = timeProvider.GetUtcNow();
        settings.RotatePublicToken(tokenGenerator.Generate(), now);

        await settingsRepository.UpdateAsync(settings, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new RotatePublicTokenResult(RotatePublicTokenStatus.Rotated, settings.PublicEventToken);
    }
}
