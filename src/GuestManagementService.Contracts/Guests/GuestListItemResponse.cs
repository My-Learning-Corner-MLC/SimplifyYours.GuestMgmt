namespace GuestManagementService.Contracts.Guests;

public sealed record GuestListItemResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string PhoneNumber,
    string? EmailAddress,
    string Gender,
    string? Relationship,
    string? Side,
    int PlusOnes,
    string? DietaryNotes,
    DateTimeOffset CreatedAt);
