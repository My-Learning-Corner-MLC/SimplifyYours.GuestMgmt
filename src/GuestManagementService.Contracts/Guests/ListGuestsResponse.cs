namespace GuestManagementService.Contracts.Guests;

public sealed record ListGuestsResponse(
    Guid EventId,
    IReadOnlyList<GuestListItemResponse> Guests,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages,
    bool HasPreviousPage,
    bool HasNextPage);
