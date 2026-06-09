namespace GuestManagementService.Contracts.Guests;

public sealed record AddGuestResponse(
    Guid Id,
    Guid EventId,
    GuestInfoResponse GuestInfo,
    DateTimeOffset CreatedAt);
