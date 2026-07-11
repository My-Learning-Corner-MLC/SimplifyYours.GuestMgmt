namespace GuestManagementService.Contracts.Seating;

public sealed record SeatingBatchOpRequest(
    string? Op,
    Guid GuestId,
    Guid? TableId,
    int? SeatIndex);
