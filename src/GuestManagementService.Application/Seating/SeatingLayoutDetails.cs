namespace GuestManagementService.Application.Seating;

public sealed record SeatingLayoutDetails(
    Guid Id,
    Guid EventId,
    IReadOnlyList<SeatingTableDetails> Tables,
    SeatingSummaryDetails Summary);
