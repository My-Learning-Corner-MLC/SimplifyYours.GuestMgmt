namespace GuestManagementService.Application.Seating.UpdateAreaPosition;

public sealed record UpdateAreaPositionResult(
    UpdateAreaPositionStatus Status,
    SeatingAreaDetails? Area)
{
    public static UpdateAreaPositionResult Updated(SeatingAreaDetails area)
    {
        return new UpdateAreaPositionResult(UpdateAreaPositionStatus.Updated, area);
    }

    public static UpdateAreaPositionResult EventNotFound()
    {
        return new UpdateAreaPositionResult(UpdateAreaPositionStatus.EventNotFound, null);
    }

    public static UpdateAreaPositionResult AreaNotFound()
    {
        return new UpdateAreaPositionResult(UpdateAreaPositionStatus.AreaNotFound, null);
    }
}
