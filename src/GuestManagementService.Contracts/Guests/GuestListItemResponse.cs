namespace GuestManagementService.Contracts.Guests;

public sealed record GuestListItemResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string PhoneNumber,
    string? EmailAddress,
    string Gender,
    // Concrete shape depends on the event's type (e.g. Guests.Wedding.WeddingGuestMetadataResponse).
    // Null when the event's type has no registered mapper.
    object? EventMetadata,
    // Free-text seating labels the organizer attached to the guest. Applies to every event type;
    // empty array when none were set.
    IReadOnlyList<string> Tags,
    DateTimeOffset CreatedAt);
