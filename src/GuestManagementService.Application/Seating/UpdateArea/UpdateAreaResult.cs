namespace GuestManagementService.Application.Seating.UpdateArea;

public sealed record UpdateAreaResult(
    UpdateAreaStatus Status,
    SeatingAreaDetails? Area)
{
    public static UpdateAreaResult Updated(SeatingAreaDetails area)
    {
        return new UpdateAreaResult(UpdateAreaStatus.Updated, area);
    }

    public static UpdateAreaResult EventNotFound()
    {
        return new UpdateAreaResult(UpdateAreaStatus.EventNotFound, null);
    }

    public static UpdateAreaResult AreaNotFound()
    {
        return new UpdateAreaResult(UpdateAreaStatus.AreaNotFound, null);
    }
}
