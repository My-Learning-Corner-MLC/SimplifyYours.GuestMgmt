namespace GuestManagementService.Contracts.Seating;

public sealed record ApplyAreaPositionsBatchRequest(
    Guid EventId,
    IReadOnlyList<AreaPositionRequest> Positions);
