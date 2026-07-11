namespace GuestManagementService.Contracts.Seating;

public sealed record ApplyTablePositionsBatchResponse(IReadOnlyList<TablePositionOpResponse> Results);
