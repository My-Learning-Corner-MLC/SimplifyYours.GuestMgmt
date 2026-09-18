namespace GuestManagementService.Application.Seating.UnassignSeat;

public sealed record UnassignSeatResult(UnassignSeatStatus Status)
{
    public static UnassignSeatResult Unassigned()
    {
        return new UnassignSeatResult(UnassignSeatStatus.Unassigned);
    }

    public static UnassignSeatResult EventNotFound()
    {
        return new UnassignSeatResult(UnassignSeatStatus.EventNotFound);
    }

    public static UnassignSeatResult TableNotFound()
    {
        return new UnassignSeatResult(UnassignSeatStatus.TableNotFound);
    }
}
