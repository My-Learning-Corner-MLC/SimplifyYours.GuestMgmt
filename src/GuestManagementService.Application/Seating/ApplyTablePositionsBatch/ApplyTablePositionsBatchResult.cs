namespace GuestManagementService.Application.Seating.ApplyTablePositionsBatch;

public sealed record ApplyTablePositionsBatchResult(
    ApplyTablePositionsBatchStatus Status,
    IReadOnlyList<TablePositionOpResult> Results)
{
    public static ApplyTablePositionsBatchResult Applied(IReadOnlyList<TablePositionOpResult> results)
    {
        return new ApplyTablePositionsBatchResult(ApplyTablePositionsBatchStatus.Applied, results);
    }

    public static ApplyTablePositionsBatchResult EventNotFound()
    {
        return new ApplyTablePositionsBatchResult(ApplyTablePositionsBatchStatus.EventNotFound, Array.Empty<TablePositionOpResult>());
    }
}
