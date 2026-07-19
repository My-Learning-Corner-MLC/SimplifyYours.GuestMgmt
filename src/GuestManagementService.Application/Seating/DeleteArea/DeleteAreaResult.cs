namespace GuestManagementService.Application.Seating.DeleteArea;

public sealed record DeleteAreaResult(DeleteAreaStatus Status)
{
    public static DeleteAreaResult Deleted()
    {
        return new DeleteAreaResult(DeleteAreaStatus.Deleted);
    }

    public static DeleteAreaResult EventNotFound()
    {
        return new DeleteAreaResult(DeleteAreaStatus.EventNotFound);
    }

    public static DeleteAreaResult AreaNotFound()
    {
        return new DeleteAreaResult(DeleteAreaStatus.AreaNotFound);
    }
}
