namespace GuestManagementService.Contracts.Seating;

public sealed record CreateTablesResponse(IReadOnlyList<SeatingTableResponse> Tables);
