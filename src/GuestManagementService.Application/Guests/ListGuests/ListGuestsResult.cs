using GuestManagementService.Application.Guests;

namespace GuestManagementService.Application.Guests.ListGuests;

public sealed record ListGuestsResult(
    ListGuestsStatus Status,
    IReadOnlyList<GuestDetails> Guests,
    int PageNumber = 0,
    int PageSize = 0,
    int TotalCount = 0,
    int TotalPages = 0,
    bool HasPreviousPage = false,
    bool HasNextPage = false)
{
    public static ListGuestsResult Found(
        IReadOnlyList<GuestDetails> guests,
        int pageNumber,
        int pageSize,
        int totalCount,
        int totalPages,
        bool hasPreviousPage,
        bool hasNextPage)
    {
        return new ListGuestsResult(
            ListGuestsStatus.Found,
            guests,
            pageNumber,
            pageSize,
            totalCount,
            totalPages,
            hasPreviousPage,
            hasNextPage);
    }

    public static ListGuestsResult EventNotFound()
    {
        return new ListGuestsResult(ListGuestsStatus.EventNotFound, Array.Empty<GuestDetails>());
    }
}
