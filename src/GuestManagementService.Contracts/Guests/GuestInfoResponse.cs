namespace GuestManagementService.Contracts.Guests;

public sealed record GuestInfoResponse(
    string FirstName,
    string LastName,
    string PhoneNumber,
    string? EmailAddress,
    string Gender,
    // Concrete shape depends on the event's type (e.g. Guests.Wedding.WeddingGuestMetadataResponse).
    // Null when the event's type has no registered mapper.
    object? EventMetadata);
