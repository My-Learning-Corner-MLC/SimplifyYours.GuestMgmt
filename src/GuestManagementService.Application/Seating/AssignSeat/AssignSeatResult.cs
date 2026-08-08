namespace GuestManagementService.Application.Seating.AssignSeat;

public sealed record AssignSeatResult(
    AssignSeatStatus Status,
    SeatingTableDetails? Table)
{
    public static AssignSeatResult Assigned(SeatingTableDetails table)
    {
        return new AssignSeatResult(AssignSeatStatus.Assigned, table);
    }

    public static AssignSeatResult EventNotFound()
    {
        return new AssignSeatResult(AssignSeatStatus.EventNotFound, null);
    }

    public static AssignSeatResult TableNotFound()
    {
        return new AssignSeatResult(AssignSeatStatus.TableNotFound, null);
    }

    public static AssignSeatResult GuestNotFound()
    {
        return new AssignSeatResult(AssignSeatStatus.GuestNotFound, null);
    }

    public static AssignSeatResult SeatIndexOutOfRange()
    {
        return new AssignSeatResult(AssignSeatStatus.SeatIndexOutOfRange, null);
    }

    public static AssignSeatResult SeatOccupied()
    {
        return new AssignSeatResult(AssignSeatStatus.SeatOccupied, null);
    }

    public static AssignSeatResult InsufficientAdjacentSeats()
    {
        return new AssignSeatResult(AssignSeatStatus.InsufficientAdjacentSeats, null);
    }
}
