namespace GuestManagementService.Application.Seating;

public sealed record SeatingSeatDetails(int SeatIndex, Guid? GuestId, string? GuestName)
{
    public static SeatingSeatDetails Empty(int seatIndex)
    {
        return new SeatingSeatDetails(seatIndex, null, null);
    }
}
