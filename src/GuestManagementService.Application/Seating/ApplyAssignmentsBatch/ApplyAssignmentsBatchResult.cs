namespace GuestManagementService.Application.Seating.ApplyAssignmentsBatch;

public sealed record ApplyAssignmentsBatchResult(
    ApplyAssignmentsBatchStatus Status,
    SeatingLayoutDetails? Layout,
    IReadOnlyList<SeatingBatchOpResult> OpResults)
{
    public static ApplyAssignmentsBatchResult Applied(
        SeatingLayoutDetails layout,
        IReadOnlyList<SeatingBatchOpResult> opResults)
    {
        return new ApplyAssignmentsBatchResult(ApplyAssignmentsBatchStatus.Applied, layout, opResults);
    }

    public static ApplyAssignmentsBatchResult EventNotFound()
    {
        return new ApplyAssignmentsBatchResult(ApplyAssignmentsBatchStatus.EventNotFound, null, Array.Empty<SeatingBatchOpResult>());
    }
}
