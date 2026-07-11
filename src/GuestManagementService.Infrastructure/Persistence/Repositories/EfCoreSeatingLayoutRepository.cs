using GuestManagementService.Application.Abstractions.Seating;
using GuestManagementService.Domain.Seating;
using Microsoft.EntityFrameworkCore;

namespace GuestManagementService.Infrastructure.Persistence.Repositories;

internal sealed class EfCoreSeatingLayoutRepository(GuestManagementServiceDbContext dbContext)
    : ISeatingLayoutRepository
{
    public async Task<SeatingLayout?> GetByEventAsync(
        Guid eventId,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        return await dbContext.SeatingLayouts
            .Include(layout => layout.Tables)
            .FirstOrDefaultAsync(
                layout => layout.EventId == eventId && layout.TenantId == tenantId,
                cancellationToken);
    }

    public async Task AddAsync(SeatingLayout layout, CancellationToken cancellationToken)
    {
        await dbContext.SeatingLayouts.AddAsync(layout, cancellationToken);
    }
}
