namespace GuestManagementService.Contracts.Invitations;

/// <summary>
/// Everything the public invitation page renders, and nothing more. Deliberately narrow: no email
/// address, phone number, guest id, tenant id, or any other guest — whoever holds the link can
/// read this without authenticating.
/// </summary>
/// <remarks>
/// The displayed content is the organiser's saved invitation, not the replicated event record, so
/// an event edited after the invitation was composed does not silently rewrite what guests see.
/// </remarks>
public sealed record GetInvitationResponse(
    string GuestName,
    /// <summary>The saved merge values, keyed by token. Shape depends on the event type.</summary>
    IReadOnlyDictionary<string, string?> Content,
    InvitationRsvpResponse Rsvp);

public sealed record InvitationRsvpResponse(
    string Status,
    int PlusOnesAllowed,
    int? PlusOnesConfirmed,
    string? DietaryNotes,
    /// <summary>Derived from the event's own date and timezone, not from the invitation's copy.</summary>
    DateTimeOffset? Deadline,
    bool IsOpen);
