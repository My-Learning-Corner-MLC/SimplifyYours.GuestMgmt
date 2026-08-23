using GuestManagementService.Application.Abstractions.EventReferences;
using GuestManagementService.Application.Abstractions.Guests;
using GuestManagementService.Application.Abstractions.Invitations;
using GuestManagementService.Domain.Invitations;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GuestManagementService.Application.Invitations.RenderInvitation;

/// <summary>
/// Resolves the render token against the two token types this endpoint still serves.
/// </summary>
/// <remarks>
/// An unknown or revoked token of either type returns the identical
/// <see cref="ResolveInvitationRenderStatus.NotFound"/> — no branch here may leak which table, if
/// any, almost matched.
/// </remarks>
public sealed class ResolveInvitationRenderQueryHandler(
    IGuestRepository guestRepository,
    IEventReferenceRepository eventReferenceRepository,
    IEventInvitationSettingsRepository settingsRepository,
    ILogger<ResolveInvitationRenderQueryHandler> logger)
    : IRequestHandler<ResolveInvitationRenderQuery, ResolveInvitationRenderResult>
{
    public async Task<ResolveInvitationRenderResult> Handle(
        ResolveInvitationRenderQuery request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
        {
            return ResolveInvitationRenderResult.NotFound();
        }

        var guest = await guestRepository.GetByInvitationTokenAsync(request.Token, cancellationToken);

        if (guest is not null)
        {
            return await ResolveGuestAsync(guest, cancellationToken);
        }

        var byPublicToken = await settingsRepository.GetByPublicEventTokenAsync(request.Token, cancellationToken);

        if (byPublicToken is not null && byPublicToken.PublicLinkEnabled)
        {
            return await ResolvePublicEventAsync(byPublicToken, cancellationToken);
        }

        logger.LogInformation("Invitation render requested for an unresolvable token.");

        return ResolveInvitationRenderResult.NotFound();
    }

    private async Task<ResolveInvitationRenderResult> ResolveGuestAsync(
        Domain.Guests.Guest guest,
        CancellationToken cancellationToken)
    {
        var eventReference = await eventReferenceRepository.GetByIdAsync(guest.EventId, cancellationToken);

        if (eventReference is null || eventReference.IsDeleted)
        {
            return ResolveInvitationRenderResult.NotFound();
        }

        var settings = await settingsRepository.GetByEventIdAsync(eventReference.EventId, cancellationToken);

        if (settings is null || settings.HtmlContent is null)
        {
            return ResolveInvitationRenderResult.NotFound();
        }

        return new ResolveInvitationRenderResult(
            ResolveInvitationRenderStatus.Guest,
            EventType: eventReference.EventType,
            GuestName: guest.FirstName,
            FieldValues: InvitationFieldValues.Parse(settings.FieldValues),
            Snapshot: ToSnapshot(settings));
    }

    private async Task<ResolveInvitationRenderResult> ResolvePublicEventAsync(
        EventInvitationSettings settings,
        CancellationToken cancellationToken)
    {
        if (settings.HtmlContent is null)
        {
            return ResolveInvitationRenderResult.NotFound();
        }

        var eventReference = await eventReferenceRepository.GetByIdAsync(settings.EventId, cancellationToken);

        if (eventReference is null || eventReference.IsDeleted)
        {
            return ResolveInvitationRenderResult.NotFound();
        }

        return new ResolveInvitationRenderResult(
            ResolveInvitationRenderStatus.PublicEvent,
            EventType: eventReference.EventType,
            GuestName: null, // never "" — see B5.
            FieldValues: InvitationFieldValues.Parse(settings.FieldValues),
            Snapshot: ToSnapshot(settings));
    }

    private static InvitationTemplateSnapshot ToSnapshot(EventInvitationSettings settings) =>
        new(settings.TemplateId!.Value, settings.TemplateVersion!.Value, settings.HtmlContent!,
            settings.CssContent, settings.JsContent);
}
