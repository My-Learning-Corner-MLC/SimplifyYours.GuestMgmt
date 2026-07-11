namespace GuestManagementService.Contracts.Seating;

public sealed record ApplyTablePositionsBatchRequest(
    Guid EventId,
    IReadOnlyList<TablePositionRequest> Positions);
