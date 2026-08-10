namespace GuestManagementService.Application.Guests.Birthday;

/// <summary>The eventMetadata shape accepted for a birthday guest, before parsing/validation.</summary>
/// <summary>
/// Dietary notes are deliberately absent — see <see cref="Wedding.WeddingGuestMetadataRequest"/>.
/// </summary>
public sealed record BirthdayGuestMetadataRequest(int? PlusOnes);
