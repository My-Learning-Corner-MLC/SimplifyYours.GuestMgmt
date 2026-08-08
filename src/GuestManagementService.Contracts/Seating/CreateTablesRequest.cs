namespace GuestManagementService.Contracts.Seating;

public sealed record CreateTablesRequest(
    Guid EventId,
    string? Name,
    string? Shape,
    int SeatCount,
    int Count = 1);
