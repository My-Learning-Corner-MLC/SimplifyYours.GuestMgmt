namespace GuestManagementService.Application.Guests.Wedding;

/// <summary>
/// The eventMetadata shape accepted for a wedding guest, before parsing/validation.
/// </summary>
/// <remarks>
/// Dietary notes are deliberately absent: they are the guest's own answer, supplied through the
/// RSVP form, not something the organiser fills in on their behalf. They remain in the stored
/// metadata and in list responses so the organiser can still read them for catering.
/// </remarks>
public sealed record WeddingGuestMetadataRequest(
    string? Relationship,
    string? Side,
    int? PlusOnes);
