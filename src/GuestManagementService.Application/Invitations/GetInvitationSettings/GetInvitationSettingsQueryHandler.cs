using GuestManagementService.Application.Abstractions.EventReferences;
using GuestManagementService.Application.Abstractions.Invitations;
using GuestManagementService.Application.Authorization;
using GuestManagementService.Domain.EventReferences;
using MediatR;

namespace GuestManagementService.Application.Invitations.GetInvitationSettings;

public sealed record GetInvitationSettingsQuery(Guid EventId) : BaseCommand, IRequest<GetInvitationSettingsResult>;

public enum GetInvitationSettingsStatus
{
    Found = 0,
    EventNotFound = 1,
}

public sealed record GetInvitationSettingsResult(
    GetInvitationSettingsStatus Status,
    string EventType = "",
    string? TemplateId = null,
    IReadOnlyDictionary<string, string?>? FieldValues = null,
    bool IsConfigured = false)
{
    public static GetInvitationSettingsResult EventNotFound() => new(GetInvitationSettingsStatus.EventNotFound);
}

/// <summary>
/// Returns the invitation an organiser has composed, or the defaults to start from.
/// </summary>
/// <remarks>
/// The <c>IsConfigured</c> flag is what lets the UI say "not yet configured" instead of implying the
/// pre-filled defaults were a deliberate choice. Without it, a never-touched event and one saved
/// with exactly the event's own values look identical.
/// </remarks>
public sealed class GetInvitationSettingsQueryHandler(
    IEventReferenceRepository eventReferenceRepository,
    IEventInvitationSettingsRepository settingsRepository)
    : IRequestHandler<GetInvitationSettingsQuery, GetInvitationSettingsResult>
{
    public async Task<GetInvitationSettingsResult> Handle(
        GetInvitationSettingsQuery request,
        CancellationToken cancellationToken)
    {
        var eventReference = await eventReferenceRepository.GetByIdAsync(request.EventId, cancellationToken);

        if (eventReference is null
            || eventReference.IsDeleted
            || eventReference.TenantId != request.CurrentUser.TenantId)
        {
            // Another tenant's event is "not found", never "forbidden" — a 403 would confirm it exists.
            return GetInvitationSettingsResult.EventNotFound();
        }

        InvitationFieldSchema.EnsureSupported(eventReference.EventType);

        var saved = await settingsRepository.GetByEventIdAsync(
            request.EventId,
            request.CurrentUser.TenantId,
            cancellationToken);

        if (saved is not null)
        {
            // Saved values win outright. Re-opening the form must show what was saved, not a fresh
            // pre-fill that would quietly discard the organiser's edits.
            //
            // IsConfigured tracks whether a real snapshot exists, not merely whether a row exists —
            // a legacy slice-1 row whose test template id did not survive the B1 migration has
            // saved field values but no snapshot, and must be treated as "not configured" so the UI
            // prompts the organiser to choose a real template.
            return new GetInvitationSettingsResult(
                GetInvitationSettingsStatus.Found,
                eventReference.EventType,
                saved.TemplateId?.ToString(),
                InvitationFieldValues.Parse(saved.FieldValues),
                IsConfigured: saved.HtmlContent is not null);
        }

        return new GetInvitationSettingsResult(
            GetInvitationSettingsStatus.Found,
            eventReference.EventType,
            TemplateId: null,
            BuildDefaults(eventReference),
            IsConfigured: false);
    }

    /// <summary>
    /// Pre-fill from the replicated event record. Couple names have no event-record source, so they
    /// start empty — that is precisely why the form exists.
    /// </summary>
    internal static Dictionary<string, string?> BuildDefaults(EventReference eventReference)
    {
        var defaults = new Dictionary<string, string?>(StringComparer.Ordinal);

        foreach (var field in InvitationFieldSchema.AllFor(eventReference.EventType))
        {
            defaults[field] = field switch
            {
                InvitationFieldSchema.EventName => eventReference.EventName,
                InvitationFieldSchema.EventDate => eventReference.EventDate?.ToString("D"),
                InvitationFieldSchema.EventTime => eventReference.EventStartTime?.ToString("t"),
                InvitationFieldSchema.VenueName => eventReference.VenueName,
                InvitationFieldSchema.VenueAddress => eventReference.VenueAddress,
                InvitationFieldSchema.VenueNotes => eventReference.VenueNotes,
                _ => null,
            };
        }

        return defaults;
    }
}
