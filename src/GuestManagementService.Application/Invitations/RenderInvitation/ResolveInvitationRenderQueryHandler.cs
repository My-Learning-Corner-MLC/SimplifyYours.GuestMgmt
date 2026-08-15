using GuestManagementService.Application.Abstractions.EventReferences;
using GuestManagementService.Application.Abstractions.Guests;
using GuestManagementService.Application.Abstractions.Invitations;
using GuestManagementService.Domain.Invitations;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GuestManagementService.Application.Invitations.RenderInvitation;

/// <summary>
/// <c>GET /guests/invitations/{token}/render[?mode=preview&amp;type=public|private]</c> resolves
/// one of three token types, each with its own rules. See the class remarks on the handler for the
/// full matrix — this record is the query, not the policy.
/// </summary>
public sealed record ResolveInvitationRenderQuery(string Token, string? Mode, string? Type)
    : IRequest<ResolveInvitationRenderResult>;

public enum ResolveInvitationRenderStatus
{
    /// <summary>Unknown, expired, or revoked token of any type — deliberately uniform.</summary>
    NotFound = 0,

    /// <summary>A token type used with a mode/type combination it does not support.</summary>
    BadRequest = 1,

    /// <summary>Guest token, no mode — slice 1's unchanged behaviour.</summary>
    Guest = 2,

    /// <summary>Public event token, no mode — guestName unbound.</summary>
    PublicEvent = 3,

    /// <summary>Preview token, mode=preview — guestName is a fixed sample or unbound per type.</summary>
    Preview = 4,
}

public sealed record ResolveInvitationRenderResult(
    ResolveInvitationRenderStatus Status,
    string? BadRequestField = null,
    string? BadRequestMessage = null,
    string EventType = "",
    /// <summary>Null means "omit guestName from the render model entirely" — see B5.</summary>
    string? GuestName = null,
    IReadOnlyDictionary<string, string?>? FieldValues = null,
    InvitationTemplateSnapshot? Snapshot = null)
{
    public static ResolveInvitationRenderResult NotFound() => new(ResolveInvitationRenderStatus.NotFound);

    public static ResolveInvitationRenderResult BadRequest(string field, string message) =>
        new(ResolveInvitationRenderStatus.BadRequest, field, message);
}

/// <summary>
/// Resolves the render token against all three token types and enforces which query-string
/// combinations each one accepts.
/// </summary>
/// <remarks>
/// <para><b>The token type decides what is permitted; the query string only refines preview.</b></para>
/// <list type="table">
/// <listheader><term>Token resolves to</term><description>mode absent / mode=preview</description></listheader>
/// <item><term>Guest</term><description>real guest data, RSVP live (unchanged) / 400</description></item>
/// <item><term>Public event</term><description>guestName unbound / 400</description></item>
/// <item><term>Preview</term><description>400 (mode required) / type=private → sample guest + saved-or-sample
/// field values; type=public → guestName unbound</description></item>
/// </list>
/// <para>
/// An unknown, expired, or revoked token of any type returns the identical <see cref="ResolveInvitationRenderStatus.NotFound"/>
/// — no branch here may leak which table, if any, almost matched.
/// </para>
/// <para>
/// A preview token can never resolve real guest data: <c>type=private</c> always binds the fixed
/// <see cref="SampleGuestName"/>, never anything read from the guest table. This handler never
/// queries <see cref="IGuestRepository"/> for a preview token at all.
/// </para>
/// </remarks>
public sealed class ResolveInvitationRenderQueryHandler(
    IGuestRepository guestRepository,
    IEventReferenceRepository eventReferenceRepository,
    IEventInvitationSettingsRepository settingsRepository,
    TimeProvider timeProvider,
    ILogger<ResolveInvitationRenderQueryHandler> logger)
    : IRequestHandler<ResolveInvitationRenderQuery, ResolveInvitationRenderResult>
{
    /// <summary>
    /// Never a real guest's name — the whole point of the preview token is that it cannot resolve
    /// one.
    /// </summary>
    public const string SampleGuestName = "Jordan Smith";

    private static readonly IReadOnlyDictionary<string, string> SampleFieldValues =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [InvitationFieldSchema.BrideName] = "Amara",
            [InvitationFieldSchema.GroomName] = "Julian",
            [InvitationFieldSchema.EventName] = "Sample Celebration",
            [InvitationFieldSchema.EventDate] = "Saturday, January 1, 2027",
            [InvitationFieldSchema.EventTime] = "18:00",
            [InvitationFieldSchema.VenueName] = "Sample Venue",
            [InvitationFieldSchema.VenueAddress] = "123 Sample Street",
            [InvitationFieldSchema.VenueNotes] = "Sample notes",
        };

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
            return await ResolveGuestAsync(guest, request.Mode, cancellationToken);
        }

        var byPublicToken = await settingsRepository.GetByPublicEventTokenAsync(request.Token, cancellationToken);

        if (byPublicToken is not null && byPublicToken.PublicLinkEnabled)
        {
            return await ResolvePublicEventAsync(byPublicToken, request.Mode, cancellationToken);
        }

        var byPreviewToken = await settingsRepository.GetByPreviewTokenAsync(request.Token, cancellationToken);

        if (byPreviewToken is not null
            && byPreviewToken.PreviewExpiresAt is { } expiresAt
            && expiresAt > timeProvider.GetUtcNow())
        {
            return await ResolvePreviewAsync(byPreviewToken, request.Mode, request.Type, cancellationToken);
        }

        logger.LogInformation("Invitation render requested for an unresolvable token.");

        return ResolveInvitationRenderResult.NotFound();
    }

    private async Task<ResolveInvitationRenderResult> ResolveGuestAsync(
        Domain.Guests.Guest guest,
        string? mode,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(mode))
        {
            // The anonymous surface must not gain a second behaviour reachable by a guest simply by
            // adding a query string nobody reviewed for guest-facing use.
            return ResolveInvitationRenderResult.BadRequest(
                "mode", "mode is not valid for a guest invitation link.");
        }

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
        string? mode,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(mode))
        {
            return ResolveInvitationRenderResult.BadRequest(
                "mode", "mode is not valid for the public event link.");
        }

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

    private async Task<ResolveInvitationRenderResult> ResolvePreviewAsync(
        EventInvitationSettings settings,
        string? mode,
        string? type,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(mode, "preview", StringComparison.OrdinalIgnoreCase))
        {
            // A preview token REQUIRES the mode param — it is never valid on its own.
            return ResolveInvitationRenderResult.BadRequest(
                "mode", "Preview links require ?mode=preview.");
        }

        var isPrivate = string.Equals(type, "private", StringComparison.OrdinalIgnoreCase);
        var isPublic = string.Equals(type, "public", StringComparison.OrdinalIgnoreCase);

        if (!isPrivate && !isPublic)
        {
            return ResolveInvitationRenderResult.BadRequest(
                "type", "type must be 'public' or 'private'.");
        }

        if (settings.HtmlContent is null)
        {
            return ResolveInvitationRenderResult.NotFound();
        }

        var eventReference = await eventReferenceRepository.GetByIdAsync(settings.EventId, cancellationToken);

        if (eventReference is null || eventReference.IsDeleted)
        {
            return ResolveInvitationRenderResult.NotFound();
        }

        var fieldValues = BuildPreviewFieldValues(settings, eventReference.EventType);

        return new ResolveInvitationRenderResult(
            ResolveInvitationRenderStatus.Preview,
            EventType: eventReference.EventType,
            // Fixed sample only — a preview token must never resolve a real guest's data.
            GuestName: isPrivate ? SampleGuestName : null,
            FieldValues: fieldValues,
            Snapshot: ToSnapshot(settings));
    }

    /// <summary>
    /// Saved (or draft) values win; anything unset falls back to a sample so preview works before
    /// anything has been saved.
    /// </summary>
    private static IReadOnlyDictionary<string, string?> BuildPreviewFieldValues(
        EventInvitationSettings settings,
        string eventType)
    {
        var saved = InvitationFieldValues.Parse(settings.FieldValues);
        var result = new Dictionary<string, string?>(StringComparer.Ordinal);

        foreach (var field in InvitationFieldSchema.AllFor(eventType))
        {
            var savedValue = saved.TryGetValue(field, out var value) ? value : null;

            result[field] = string.IsNullOrWhiteSpace(savedValue)
                ? SampleFieldValues.GetValueOrDefault(field)
                : savedValue;
        }

        return result;
    }

    private static InvitationTemplateSnapshot ToSnapshot(EventInvitationSettings settings) =>
        new(settings.TemplateId!.Value, settings.TemplateVersion!.Value, settings.HtmlContent!,
            settings.CssContent, settings.JsContent);
}
