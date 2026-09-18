namespace GuestManagementService.Application.Seating.UpdateTable;

public sealed record UpdateTableResult(
    UpdateTableStatus Status,
    SeatingTableDetails? Table)
{
    public static UpdateTableResult Updated(SeatingTableDetails table)
    {
        return new UpdateTableResult(UpdateTableStatus.Updated, table);
    }

    public static UpdateTableResult EventNotFound()
    {
        return new UpdateTableResult(UpdateTableStatus.EventNotFound, null);
    }

    public static UpdateTableResult TableNotFound()
    {
        return new UpdateTableResult(UpdateTableStatus.TableNotFound, null);
    }

    public static UpdateTableResult SeatCountBelowOccupiedSeats()
    {
        return new UpdateTableResult(UpdateTableStatus.SeatCountBelowOccupiedSeats, null);
    }
}
