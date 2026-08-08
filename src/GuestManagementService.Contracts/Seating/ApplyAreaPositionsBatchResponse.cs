namespace GuestManagementService.Contracts.Seating;

public sealed record ApplyAreaPositionsBatchResponse(IReadOnlyList<AreaPositionOpResponse> Results);
