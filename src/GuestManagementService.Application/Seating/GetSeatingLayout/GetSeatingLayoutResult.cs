namespace GuestManagementService.Application.Seating.GetSeatingLayout;

public sealed record GetSeatingLayoutResult(
    GetSeatingLayoutStatus Status,
    SeatingLayoutDetails? Layout)
{
    public static GetSeatingLayoutResult Found(SeatingLayoutDetails layout)
    {
        return new GetSeatingLayoutResult(GetSeatingLayoutStatus.Found, layout);
    }

    public static GetSeatingLayoutResult EventNotFound()
    {
        return new GetSeatingLayoutResult(GetSeatingLayoutStatus.EventNotFound, null);
    }
}
