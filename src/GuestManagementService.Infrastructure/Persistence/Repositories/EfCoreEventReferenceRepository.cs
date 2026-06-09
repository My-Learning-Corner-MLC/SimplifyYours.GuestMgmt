using GuestManagementService.Application.Abstractions.EventReferences;
using GuestManagementService.Domain.EventReferences;
using Microsoft.EntityFrameworkCore;

namespace GuestManagementService.Infrastructure.Persistence.Repositories;

internal sealed class EfCoreEventReferenceRepository(GuestManagementServiceDbContext dbContext)
    : IEventReferenceRepository
{
    public async Task<EventReference?> GetByIdAsync(Guid eventId, CancellationToken cancellationToken)
    {
        return await dbContext.EventReferences
            .SingleOrDefaultAsync(reference => reference.EventId == eventId, cancellationToken);
    }

    public async Task UpsertAsync(EventReference eventReference, CancellationToken cancellationToken)
    {
        if (dbContext.Entry(eventReference).State == EntityState.Detached)
        {
            var exists = await dbContext.EventReferences
                .AsNoTracking()
                .AnyAsync(reference => reference.EventId == eventReference.EventId, cancellationToken);

            if (exists)
            {
                dbContext.EventReferences.Update(eventReference);
                return;
            }

            await dbContext.EventReferences.AddAsync(eventReference, cancellationToken);
        }
    }
}
