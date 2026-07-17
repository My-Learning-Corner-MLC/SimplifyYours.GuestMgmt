namespace GuestManagementService.Application.Guests.Birthday;

/// <summary>The eventMetadata shape accepted for a birthday guest, before parsing/validation.</summary>
public sealed record BirthdayGuestMetadataRequest(int? PlusOnes, string? DietaryNotes);
