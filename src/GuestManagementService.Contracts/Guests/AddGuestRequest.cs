namespace GuestManagementService.Contracts.Guests;

public sealed record AddGuestRequest(
    Guid EventId,
    GuestInfoRequest GuestInfo);
