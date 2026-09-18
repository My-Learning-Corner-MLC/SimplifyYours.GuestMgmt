namespace GuestManagementService.Application.Seating.UpdateTablePosition;

public sealed record UpdateTablePositionResult(
    UpdateTablePositionStatus Status,
    SeatingTableDetails? Table)
{
    public static UpdateTablePositionResult Updated(SeatingTableDetails table)
    {
        return new UpdateTablePositionResult(UpdateTablePositionStatus.Updated, table);
    }

    public static UpdateTablePositionResult EventNotFound()
    {
        return new UpdateTablePositionResult(UpdateTablePositionStatus.EventNotFound, null);
    }

    public static UpdateTablePositionResult TableNotFound()
    {
        return new UpdateTablePositionResult(UpdateTablePositionStatus.TableNotFound, null);
    }
}
