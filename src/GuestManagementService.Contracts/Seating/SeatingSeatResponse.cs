namespace GuestManagementService.Contracts.Seating;

public sealed record SeatingSeatResponse(int SeatIndex, Guid? GuestId, string? GuestName);
