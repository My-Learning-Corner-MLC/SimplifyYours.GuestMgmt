namespace GuestManagementService.Application.Seating;

public sealed record SeatingLayoutDetails(
    Guid Id,
    Guid EventId,
    IReadOnlyList<SeatingTableDetails> Tables,
    IReadOnlyList<SeatingAreaDetails> Areas,
    SeatingSummaryDetails Summary);
