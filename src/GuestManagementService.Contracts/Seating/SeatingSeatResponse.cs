namespace GuestManagementService.Contracts.Seating;

public sealed record SeatingSeatResponse(
    int SeatIndex,
    Guid? GuestId,
    string? GuestName,
    bool IsReservedForParty,
    Guid? PartyOwnerGuestId);
