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
    // Two independent axes: a guest can respond through a link the organiser copied by hand, with
    // no invitation ever sent, so delivery and response must not collapse into one value.
    string DeliveryStatus,
    string RsvpStatus,
    DateTimeOffset? RespondedAt,
    int? PlusOnesConfirmed,
    DateTimeOffset CreatedAt);
