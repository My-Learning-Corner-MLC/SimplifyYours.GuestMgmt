namespace GuestManagementService.Contracts.Guests;

public sealed record QueryGuestsRequest(
    Guid EventId,
    int? PageNumber,
    int? PageSize,
    string? Search,
    string? SortBy,
    string? SortDirection);
