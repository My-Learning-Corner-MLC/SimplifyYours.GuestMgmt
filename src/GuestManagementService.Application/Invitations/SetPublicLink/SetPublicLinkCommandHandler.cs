using FluentValidation;
using FluentValidation.Results;
using GuestManagementService.Application.Abstractions.Common;
using GuestManagementService.Application.Abstractions.EventReferences;
using GuestManagementService.Application.Abstractions.Guests;
using GuestManagementService.Application.Abstractions.Invitations;
using GuestManagementService.Application.Authorization;
using MediatR;

namespace GuestManagementService.Application.Invitations.SetPublicLink;

/// <summary>Toggles the event-level public invitation link. Default off.</summary>
public sealed record SetPublicLinkCommand(Guid EventId, bool Enabled) : BaseCommand, IRequest<SetPublicLinkResult>;

public enum SetPublicLinkStatus
{
    Updated = 0,
    EventNotFound = 1,
}

public sealed record SetPublicLinkResult(
    SetPublicLinkStatus Status,
    bool Enabled = false,
    string? PublicEventToken = null)
{
    public static SetPublicLinkResult EventNotFound() => new(SetPublicLinkStatus.EventNotFound);
}

/// <summary>
/// Enables or disables the event's public link. Owner-scoped: another tenant's (or another
/// organiser's) event is "not found", never "forbidden" — a 403 would confirm it exists, which is
/// the same reasoning already applied throughout <see cref="Invitations"/>.
/// </summary>
public sealed class SetPublicLinkCommandHandler(
    IEventReferenceRepository eventReferenceRepository,
    IEventInvitationSettingsRepository settingsRepository,
    IInvitationTokenGenerator tokenGenerator,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : IRequestHandler<SetPublicLinkCommand, SetPublicLinkResult>
{
    public async Task<SetPublicLinkResult> Handle(SetPublicLinkCommand request, CancellationToken cancellationToken)
    {
        var eventReference = await eventReferenceRepository.GetByIdAsync(request.EventId, cancellationToken);

        if (eventReference is null
            || eventReference.IsDeleted
            || eventReference.TenantId != request.CurrentUser.TenantId)
        {
            return SetPublicLinkResult.EventNotFound();
        }

        var settings = await settingsRepository.GetByEventIdAsync(
            request.EventId,
            request.CurrentUser.TenantId,
            cancellationToken);

        if (settings is null || settings.HtmlContent is null)
        {
            throw new ValidationException(
            [
                new ValidationFailure(
                    nameof(SetPublicLinkCommand.Enabled),
                    "Choose and save a template before enabling the public link."),
            ]);
        }

        var now = timeProvider.GetUtcNow();

        if (request.Enabled)
        {
            settings.EnablePublicLink(tokenGenerator.Generate, now);
        }
        else
        {
            settings.DisablePublicLink(now);
        }

        await settingsRepository.UpdateAsync(settings, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new SetPublicLinkResult(SetPublicLinkStatus.Updated, settings.PublicLinkEnabled, settings.PublicEventToken);
    }
}
