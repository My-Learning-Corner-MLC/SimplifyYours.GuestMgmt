namespace GuestManagementService.Application.Seating;

public sealed record SeatingSeatDetails(
    int SeatIndex,
    Guid? GuestId,
    string? GuestName,
    bool IsReservedForParty,
    Guid? PartyOwnerGuestId)
{
    public static SeatingSeatDetails Empty(int seatIndex)
    {
        return new SeatingSeatDetails(seatIndex, null, null, false, null);
    }
}
