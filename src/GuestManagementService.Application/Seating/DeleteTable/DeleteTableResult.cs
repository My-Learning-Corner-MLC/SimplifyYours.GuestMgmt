namespace GuestManagementService.Application.Seating.DeleteTable;

public sealed record DeleteTableResult(DeleteTableStatus Status)
{
    public static DeleteTableResult Deleted()
    {
        return new DeleteTableResult(DeleteTableStatus.Deleted);
    }

    public static DeleteTableResult EventNotFound()
    {
        return new DeleteTableResult(DeleteTableStatus.EventNotFound);
    }

    public static DeleteTableResult TableNotFound()
    {
        return new DeleteTableResult(DeleteTableStatus.TableNotFound);
    }
}
