using GuestManagementService.Application.Abstractions.Guests;
using GuestManagementService.Application.Guests.ListGuests;
using GuestManagementService.Domain.Guests;

namespace GuestManagementService.Application.Seating;

/// <summary>
/// Loads every guest for an event, paging through <see cref="IGuestRepository.ListAsync"/> at
/// its max page size. Seating needs the full roster (to project floating/seated guests and
/// validate assignment targets) — never a UI-facing page — so this exists once here rather than
/// each handler re-deriving its own pagination loop.
/// </summary>
internal static class GuestRoster
{
    public static async Task<IReadOnlyList<Guest>> LoadAllAsync(
        IGuestRepository guestRepository,
        Guid eventId,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var guests = new List<Guest>();
        var pageNumber = 1;

        while (true)
        {
            var page = await guestRepository.ListAsync(
                new GuestListQueryOptions(
                    eventId,
                    tenantId,
                    pageNumber,
                    ListGuestsQueryDefaults.MaxPageSize,
                    Search: null,
                    GuestSortField.CreatedAt,
                    SortDirection.Asc),
                cancellationToken);

            guests.AddRange(page.Items);

            if (guests.Count >= page.TotalCount || page.Items.Count == 0)
            {
                break;
            }

            pageNumber++;
        }

        return guests;
    }
}
