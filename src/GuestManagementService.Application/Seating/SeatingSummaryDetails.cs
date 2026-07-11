namespace GuestManagementService.Application.Seating;

public sealed record SeatingSummaryDetails(
    int TableCount,
    int SeatCount,
    int SeatedCount,
    int FloatingCount);
