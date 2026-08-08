namespace GuestManagementService.Contracts.Seating;

public sealed record ApplyAssignmentsBatchResponse(
    SeatingLayoutResponse Layout,
    IReadOnlyList<SeatingBatchOpResponse> OpResults);
