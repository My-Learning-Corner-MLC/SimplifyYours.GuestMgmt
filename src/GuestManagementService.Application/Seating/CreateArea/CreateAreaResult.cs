namespace GuestManagementService.Application.Seating.CreateArea;

public sealed record CreateAreaResult(
    CreateAreaStatus Status,
    SeatingAreaDetails? Area)
{
    public static CreateAreaResult Created(SeatingAreaDetails area)
    {
        return new CreateAreaResult(CreateAreaStatus.Created, area);
    }

    public static CreateAreaResult EventNotFound()
    {
        return new CreateAreaResult(CreateAreaStatus.EventNotFound, null);
    }
}
