using FluentValidation;
using FluentValidation.Results;
using GuestManagementService.Application.Abstractions.Common;
using GuestManagementService.Application.Abstractions.EventReferences;
using GuestManagementService.Application.Abstractions.Guests;
using GuestManagementService.Application.Abstractions.Invitations;
using GuestManagementService.Application.Authorization;
using MediatR;

namespace GuestManagementService.Application.Invitations.RotatePublicToken;

/// <summary>
/// Replaces the public event token so the previously shared URL immediately 404s, while the link
/// stays enabled at a new URL. Distinct from disabling: that turns the link off, this issues a
/// fresh one. Requires the link to already be enabled — rotating a disabled link is meaningless.
/// </summary>
public sealed record RotatePublicTokenCommand(Guid EventId) : BaseCommand, IRequest<RotatePublicTokenResult>;

public enum RotatePublicTokenStatus
{
    Rotated = 0,
    EventNotFound = 1,
}

public sealed record RotatePublicTokenResult(RotatePublicTokenStatus Status, string? PublicEventToken = null)
{
    public static RotatePublicTokenResult EventNotFound() => new(RotatePublicTokenStatus.EventNotFound);
}

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
