namespace GuestManagementService.Contracts.Seating;

public sealed record UpdateTableRequest(
    Guid EventId,
    string? Name,
    string? Shape,
    int SeatCount,
    bool IsFull);
