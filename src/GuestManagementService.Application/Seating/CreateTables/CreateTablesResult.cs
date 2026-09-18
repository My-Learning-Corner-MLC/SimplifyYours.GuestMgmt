namespace GuestManagementService.Application.Seating.CreateTables;

public sealed record CreateTablesResult(
    CreateTablesStatus Status,
    IReadOnlyList<SeatingTableDetails> Tables)
{
    public static CreateTablesResult Created(IReadOnlyList<SeatingTableDetails> tables)
    {
        return new CreateTablesResult(CreateTablesStatus.Created, tables);
    }

    public static CreateTablesResult EventNotFound()
    {
        return new CreateTablesResult(CreateTablesStatus.EventNotFound, Array.Empty<SeatingTableDetails>());
    }
}
