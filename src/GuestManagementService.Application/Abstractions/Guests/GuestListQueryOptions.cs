namespace GuestManagementService.Application.Abstractions.Guests;

public sealed record GuestListQueryOptions(
    Guid EventId,
    Guid TenantId,
    int PageNumber,
    int PageSize,
    string? Search,
    GuestSortField SortBy,
    SortDirection SortDirection);
