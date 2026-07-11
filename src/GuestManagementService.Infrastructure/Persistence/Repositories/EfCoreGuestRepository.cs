using GuestManagementService.Application.Abstractions.Guests;
using GuestManagementService.Domain.Guests;
using Microsoft.EntityFrameworkCore;

namespace GuestManagementService.Infrastructure.Persistence.Repositories;

internal sealed class EfCoreGuestRepository(GuestManagementServiceDbContext dbContext) : IGuestRepository
{
    public async Task AddAsync(Guest guest, CancellationToken cancellationToken)
    {
        await dbContext.Guests.AddAsync(guest, cancellationToken);
    }

    public async Task<IReadOnlyList<Guest>> ListByEventAsync(
        Guid eventId,
        CancellationToken cancellationToken)
    {
        return await dbContext.Guests
            .AsNoTracking()
            .Where(guest => guest.EventId == eventId)
            .OrderBy(guest => guest.CreatedAt)
            .ThenBy(guest => guest.Id)
            .ToListAsync(cancellationToken);
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

    public async Task<bool> ExistsAsync(
        Guid eventId,
        Guid guestId,
        CancellationToken cancellationToken)
    {
        return await dbContext.Guests
            .AsNoTracking()
            .AnyAsync(guest => guest.EventId == eventId && guest.Id == guestId, cancellationToken);
    }
}
