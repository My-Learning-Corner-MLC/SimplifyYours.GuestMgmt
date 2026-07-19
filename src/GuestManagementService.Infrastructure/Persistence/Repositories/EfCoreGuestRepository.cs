using GuestManagementService.Application.Abstractions.Guests;
using GuestManagementService.Application.Guests.ListGuests;
using GuestManagementService.Domain.Guests;
using Microsoft.EntityFrameworkCore;

namespace GuestManagementService.Infrastructure.Persistence.Repositories;

internal sealed class EfCoreGuestRepository(GuestManagementServiceDbContext dbContext) : IGuestRepository
{
    public async Task AddAsync(Guest guest, CancellationToken cancellationToken)
    {
        await dbContext.Guests.AddAsync(guest, cancellationToken);
    }

    public async Task<GuestListPage> ListAsync(GuestListQueryOptions options, CancellationToken cancellationToken)
    {
        var query = GuestListQueryBuilder.ApplyFilters(dbContext.Guests.AsNoTracking(), options);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await GuestListQueryBuilder.ApplySorting(query, options.SortBy, options.SortDirection)
            .Skip((options.PageNumber - 1) * options.PageSize)
            .Take(options.PageSize)
            .ToListAsync(cancellationToken);

        return new GuestListPage(items, options.PageNumber, options.PageSize, totalCount);
    }

    public async Task<Guest?> GetByIdAsync(
        Guid eventId,
        Guid guestId,
        CancellationToken cancellationToken)
    {
        return await dbContext.Guests
            .AsNoTracking()
            .FirstOrDefaultAsync(guest => guest.EventId == eventId && guest.Id == guestId, cancellationToken);
    }

    public async Task<bool> ExistsByPhoneAsync(
        Guid eventId,
        string normalizedPhoneNumber,
        CancellationToken cancellationToken)
    {
        return await dbContext.Guests
            .AsNoTracking()
            .AnyAsync(
                guest => guest.EventId == eventId
                    && guest.NormalizedPhoneNumber == normalizedPhoneNumber,
                cancellationToken);
    }

    public async Task<bool> ExistsByEmailAsync(
        Guid eventId,
        string normalizedEmailAddress,
        CancellationToken cancellationToken)
    {
        return await dbContext.Guests
            .AsNoTracking()
            .AnyAsync(
                guest => guest.EventId == eventId
                    && guest.NormalizedEmailAddress == normalizedEmailAddress,
                cancellationToken);
    }
}
