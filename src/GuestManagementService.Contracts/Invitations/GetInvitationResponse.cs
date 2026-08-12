namespace GuestManagementService.Contracts.Invitations;

/// <summary>
/// Everything the public invitation page renders, and nothing more. Deliberately narrow: no email
/// address, phone number, guest id, tenant id, or any other guest — whoever holds the link can
/// read this without authenticating.
/// </summary>
public sealed record GetInvitationResponse(
    string GuestName,
    InvitationEventResponse Event,
    InvitationRsvpResponse Rsvp);

public sealed record InvitationEventResponse(
    string Name,
    DateOnly? Date,
    TimeOnly? StartTime,
    string? TimeZoneId,
    InvitationVenueResponse? Venue);

public sealed record InvitationVenueResponse(string? Name, string? Address, string? Notes);

public sealed record InvitationRsvpResponse(
    string Status,
    int PlusOnesAllowed,
    int? PlusOnesConfirmed,
    string? DietaryNotes,
    DateTimeOffset? Deadline,
    bool IsOpen);
