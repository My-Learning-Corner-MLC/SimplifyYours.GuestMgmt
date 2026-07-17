namespace GuestManagementService.Contracts.Guests.Birthday;

public sealed record BirthdayGuestMetadataResponse(
    int PlusOnes,
    string? DietaryNotes);
