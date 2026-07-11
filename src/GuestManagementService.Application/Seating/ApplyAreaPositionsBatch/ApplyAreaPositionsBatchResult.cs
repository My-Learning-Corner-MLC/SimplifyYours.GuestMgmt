namespace GuestManagementService.Application.Seating.ApplyAreaPositionsBatch;

public sealed record ApplyAreaPositionsBatchResult(
    ApplyAreaPositionsBatchStatus Status,
    IReadOnlyList<AreaPositionOpResult> Results)
{
    public static ApplyAreaPositionsBatchResult Applied(IReadOnlyList<AreaPositionOpResult> results)
    {
        return new ApplyAreaPositionsBatchResult(ApplyAreaPositionsBatchStatus.Applied, results);
    }

    public static ApplyAreaPositionsBatchResult EventNotFound()
    {
        return new ApplyAreaPositionsBatchResult(ApplyAreaPositionsBatchStatus.EventNotFound, Array.Empty<AreaPositionOpResult>());
    }
}
