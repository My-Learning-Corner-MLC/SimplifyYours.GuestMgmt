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

    public async Task<Guest?> GetByInvitationTokenAsync(string invitationToken, CancellationToken cancellationToken)
    {
        // Tracked, not AsNoTracking: the RSVP write path mutates the entity it loads here.
        return await dbContext.Guests
            .SingleOrDefaultAsync(guest => guest.InvitationToken == invitationToken, cancellationToken);
    }

    public Task UpdateAsync(Guest guest, CancellationToken cancellationToken)
    {
        dbContext.Guests.Update(guest);

        return Task.CompletedTask;
    }

    public async Task<Guest?> GetByIdAsync(Guid guestId, Guid tenantId, CancellationToken cancellationToken)
    {
        return await dbContext.Guests
            .AsNoTracking()
            .SingleOrDefaultAsync(
                guest => guest.Id == guestId && guest.TenantId == tenantId,
                cancellationToken);
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
