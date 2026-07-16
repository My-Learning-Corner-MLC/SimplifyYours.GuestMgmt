namespace GuestManagementService.Contracts.Guests;

public sealed record GuestInfoRequest(
    string? FirstName,
    string? LastName,
    string? PhoneNumber,
    string? EmailAddress,
    string? Gender,
    string? Relationship = null,
    string? Side = null,
    int? PlusOnes = null,
    string? DietaryNotes = null);
