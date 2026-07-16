namespace GuestManagementService.Contracts.Guests;

public sealed record QueryGuestsResponse(
    Guid EventId,
    IReadOnlyCollection<GuestListItemResponse> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages,
    bool HasPreviousPage,
    bool HasNextPage);
