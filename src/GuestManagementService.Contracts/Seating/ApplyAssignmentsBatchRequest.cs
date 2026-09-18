namespace GuestManagementService.Contracts.Seating;

public sealed record ApplyAssignmentsBatchRequest(
    Guid EventId,
    IReadOnlyList<SeatingBatchOpRequest> Ops);
