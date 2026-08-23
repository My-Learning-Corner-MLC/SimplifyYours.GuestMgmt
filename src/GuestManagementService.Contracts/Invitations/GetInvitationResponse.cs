namespace GuestManagementService.Contracts.Invitations;

/// <summary>
/// Everything the invitation page's chrome needs outside the rendered invitation itself —
/// deliberately narrow: no email address, phone number, guest id, tenant id, or any other guest —
/// whoever holds the link can read this without authenticating.
/// </summary>
/// <remarks>
/// The invitation's own content (merge values rendered into the template) is served separately by
/// the <c>/render</c> endpoint, which the page loads into an iframe — this response only carries
/// what drives the page around it: the guest's name and their RSVP state.
/// </remarks>
public sealed record GetInvitationResponse(
    string GuestName,
    InvitationRsvpResponse Rsvp);

public sealed record InvitationRsvpResponse(
    string Status,
    int PlusOnesAllowed,
    int? PlusOnesConfirmed,
    string? DietaryNotes,
    /// <summary>Derived from the event's own date and timezone, not from the invitation's copy.</summary>
    DateTimeOffset? Deadline,
    bool IsOpen,
    /// <summary>
    /// When the guest first answered — the date in the page's "You responded" chip. Null until they
    /// answer, and it records the first response only: editing an answer does not move it.
    /// </summary>
    DateTimeOffset? RespondedAt);
