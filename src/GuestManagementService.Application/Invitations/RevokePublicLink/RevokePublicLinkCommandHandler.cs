using FluentValidation;
using FluentValidation.Results;
using GuestManagementService.Application.Abstractions.Common;
using GuestManagementService.Application.Abstractions.EventReferences;
using GuestManagementService.Application.Abstractions.Guests;
using GuestManagementService.Application.Abstractions.Invitations;
using GuestManagementService.Application.Authorization;
using MediatR;

namespace GuestManagementService.Application.Invitations.RevokePublicLink;

/// <summary>Rotates the public event token so the previously shared URL immediately 404s.</summary>
public sealed record RevokePublicLinkCommand(Guid EventId) : BaseCommand, IRequest<RevokePublicLinkResult>;

public enum RevokePublicLinkStatus
{
    Revoked = 0,
    EventNotFound = 1,
}

public sealed record RevokePublicLinkResult(RevokePublicLinkStatus Status, string? PublicEventToken = null)
{
    public static RevokePublicLinkResult EventNotFound() => new(RevokePublicLinkStatus.EventNotFound);
}

/// <summary>Owner-scoped, same reasoning as <c>SetPublicLinkCommandHandler</c>.</summary>
public sealed class RevokePublicLinkCommandHandler(
    IEventReferenceRepository eventReferenceRepository,
    IEventInvitationSettingsRepository settingsRepository,
    IInvitationTokenGenerator tokenGenerator,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : IRequestHandler<RevokePublicLinkCommand, RevokePublicLinkResult>
{
    public async Task<RevokePublicLinkResult> Handle(
        RevokePublicLinkCommand request,
        CancellationToken cancellationToken)
    {
        var eventReference = await eventReferenceRepository.GetByIdAsync(request.EventId, cancellationToken);

        if (eventReference is null
            || eventReference.IsDeleted
            || eventReference.TenantId != request.CurrentUser.TenantId)
        {
            return RevokePublicLinkResult.EventNotFound();
        }

        var settings = await settingsRepository.GetByEventIdAsync(
            request.EventId,
            request.CurrentUser.TenantId,
            cancellationToken);

        if (settings is null || string.IsNullOrEmpty(settings.PublicEventToken))
        {
            throw new ValidationException(
            [
                new ValidationFailure(
                    nameof(RevokePublicLinkCommand.EventId),
                    "The public link has not been enabled yet."),
            ]);
        }

        var now = timeProvider.GetUtcNow();
        settings.RevokePublicLink(tokenGenerator.Generate(), now);

        await settingsRepository.UpdateAsync(settings, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new RevokePublicLinkResult(RevokePublicLinkStatus.Revoked, settings.PublicEventToken);
    }
}
