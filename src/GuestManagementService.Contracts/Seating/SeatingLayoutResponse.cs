namespace GuestManagementService.Contracts.Seating;

public sealed record SeatingLayoutResponse(
    Guid EventId,
    IReadOnlyList<SeatingTableResponse> Tables,
    SeatingSummaryResponse Summary);
