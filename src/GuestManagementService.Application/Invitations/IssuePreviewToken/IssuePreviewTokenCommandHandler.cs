using FluentValidation;
using FluentValidation.Results;
using GuestManagementService.Application.Abstractions.Common;
using GuestManagementService.Application.Abstractions.EventReferences;
using GuestManagementService.Application.Abstractions.Guests;
using GuestManagementService.Application.Abstractions.Invitations;
using GuestManagementService.Application.Authorization;
using MediatR;

namespace GuestManagementService.Application.Invitations.IssuePreviewToken;

/// <summary>
/// Mints a short-lived preview token for the caller's own event, so the frontend can build a
/// preview URL for an <c>&lt;iframe&gt;</c> that cannot carry an <c>Authorization</c> header.
/// </summary>
public sealed record IssuePreviewTokenCommand(Guid EventId) : BaseCommand, IRequest<IssuePreviewTokenResult>;

public enum IssuePreviewTokenStatus
{
    Issued = 0,
    EventNotFound = 1,
}

public sealed record IssuePreviewTokenResult(
    IssuePreviewTokenStatus Status,
    string? Token = null,
    DateTimeOffset? ExpiresAt = null)
{
    public static IssuePreviewTokenResult EventNotFound() => new(IssuePreviewTokenStatus.EventNotFound);
}

/// <summary>
/// Owner-scoped, same reasoning as the public-link handlers. Re-issuing overwrites the previous
/// preview token, invalidating it immediately.
/// </summary>
public sealed class IssuePreviewTokenCommandHandler(
    IEventReferenceRepository eventReferenceRepository,
    IEventInvitationSettingsRepository settingsRepository,
    IInvitationTokenGenerator tokenGenerator,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : IRequestHandler<IssuePreviewTokenCommand, IssuePreviewTokenResult>
{
    /// <summary>
    /// 15 minutes: long enough to review a preview pane without re-issuing mid-review, short enough
    /// that a leaked preview URL (e.g. pasted into a chat while debugging) stops working quickly. It
    /// only ever renders sample/draft data, never a real guest's, so the blast radius of a leak is
    /// small regardless — this is generosity for the organiser's workflow, not a security backstop.
    /// </summary>
    public static readonly TimeSpan PreviewTokenLifetime = TimeSpan.FromMinutes(15);

    public async Task<IssuePreviewTokenResult> Handle(
        IssuePreviewTokenCommand request,
        CancellationToken cancellationToken)
    {
        var eventReference = await eventReferenceRepository.GetByIdAsync(request.EventId, cancellationToken);

        if (eventReference is null
            || eventReference.IsDeleted
            || eventReference.TenantId != request.CurrentUser.TenantId)
        {
            return IssuePreviewTokenResult.EventNotFound();
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
                    nameof(IssuePreviewTokenCommand.EventId),
                    "Choose and save a template before previewing."),
            ]);
        }

        var now = timeProvider.GetUtcNow();
        var expiresAt = now.Add(PreviewTokenLifetime);

        settings.IssuePreviewToken(tokenGenerator.Generate(), expiresAt, now);

        await settingsRepository.UpdateAsync(settings, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new IssuePreviewTokenResult(IssuePreviewTokenStatus.Issued, settings.PreviewToken, settings.PreviewExpiresAt);
    }
}
