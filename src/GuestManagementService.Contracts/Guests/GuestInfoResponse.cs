namespace GuestManagementService.Contracts.Guests;

public sealed record GuestInfoResponse(
    string FirstName,
    string LastName,
    string PhoneNumber,
    string? EmailAddress,
    string Gender);
