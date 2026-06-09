using GuestManagementService.Application.Abstractions.Common;

namespace GuestManagementService.Infrastructure.Persistence;

internal sealed class EfCoreUnitOfWork(GuestManagementServiceDbContext dbContext) : IUnitOfWork
{
    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
