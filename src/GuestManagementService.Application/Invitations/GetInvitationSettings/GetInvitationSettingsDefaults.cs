using GuestManagementService.Application.Abstractions.Invitations;
using GuestManagementService.Domain.EventReferences;

namespace GuestManagementService.Application.Invitations.GetInvitationSettings;

internal static class GetInvitationSettingsDefaults
{
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
