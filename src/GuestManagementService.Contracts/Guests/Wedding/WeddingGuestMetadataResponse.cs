namespace GuestManagementService.Contracts.Guests.Wedding;

public sealed record WeddingGuestMetadataResponse(
    string? Relationship,
    string? Side,
    int PlusOnes,
    string? DietaryNotes,
    IReadOnlyList<string> Tags);
