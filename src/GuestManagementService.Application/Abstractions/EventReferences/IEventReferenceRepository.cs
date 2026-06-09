using GuestManagementService.Domain.EventReferences;

namespace GuestManagementService.Application.Abstractions.EventReferences;

public interface IEventReferenceRepository
{
    Task<EventReference?> GetByIdAsync(Guid eventId, CancellationToken cancellationToken);

    Task UpsertAsync(EventReference eventReference, CancellationToken cancellationToken);
}
