namespace GuestManagementService.Application.Guests.Wedding;

/// <summary>The eventMetadata shape accepted for a wedding guest, before parsing/validation.</summary>
public sealed record WeddingGuestMetadataRequest(
    string? Relationship,
    string? Side,
    int? PlusOnes,
    string? DietaryNotes);
