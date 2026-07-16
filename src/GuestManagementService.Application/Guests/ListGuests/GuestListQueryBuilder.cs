using GuestManagementService.Application.Abstractions.Guests;
using GuestManagementService.Domain.Guests;

namespace GuestManagementService.Application.Guests.ListGuests;

public static class GuestListQueryBuilder
{
    public static IQueryable<Guest> ApplyFilters(IQueryable<Guest> query, GuestListQueryOptions options)
    {
        query = query.Where(guest => guest.EventId == options.EventId);
        query = query.Where(guest => guest.TenantId == options.TenantId);
        query = ApplySearch(query, options.Search);

        return query;
    }

    public static IOrderedQueryable<Guest> ApplySorting(
        IQueryable<Guest> query,
        GuestSortField sortBy,
        SortDirection sortDirection)
    {
        return (sortBy, sortDirection) switch
        {
            (GuestSortField.Name, SortDirection.Asc) => query
                .OrderBy(guest => guest.LastName)
                .ThenBy(guest => guest.FirstName)
                .ThenBy(guest => guest.Id),
            (GuestSortField.Name, SortDirection.Desc) => query
                .OrderByDescending(guest => guest.LastName)
                .ThenByDescending(guest => guest.FirstName)
                .ThenBy(guest => guest.Id),
            (GuestSortField.Email, SortDirection.Asc) => query
                .OrderBy(guest => guest.EmailAddress)
                .ThenBy(guest => guest.Id),
            (GuestSortField.Email, SortDirection.Desc) => query
                .OrderByDescending(guest => guest.EmailAddress)
                .ThenBy(guest => guest.Id),
            (GuestSortField.CreatedAt, SortDirection.Asc) => query
                .OrderBy(guest => guest.CreatedAt)
                .ThenBy(guest => guest.Id),
            _ => query
                .OrderByDescending(guest => guest.CreatedAt)
                .ThenBy(guest => guest.Id)
        };
    }

    private static IQueryable<Guest> ApplySearch(IQueryable<Guest> query, string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return query;
        }

        var normalizedSearch = search.Trim().ToLowerInvariant();

        return query.Where(guest =>
            guest.FirstName.ToLower().Contains(normalizedSearch)
            || guest.LastName.ToLower().Contains(normalizedSearch)
            || (guest.EmailAddress != null && guest.EmailAddress.ToLower().Contains(normalizedSearch)));
    }
}
