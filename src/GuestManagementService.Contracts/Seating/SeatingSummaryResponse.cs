namespace GuestManagementService.Contracts.Seating;

public sealed record SeatingSummaryResponse(
    int TableCount,
    int SeatCount,
    int SeatedCount,
    int FloatingCount);
