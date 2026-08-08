using GuestManagementService.Domain.Seating;

namespace GuestManagementService.Application.Abstractions.Seating;

public interface ISeatingLayoutRepository
{
    // Returns the tracked layout (with its tables) for an event within a tenant, or null
    // when none has been created yet.
    Task<SeatingLayout?> GetByEventAsync(
        Guid eventId,
        Guid tenantId,
        CancellationToken cancellationToken);

    Task AddAsync(SeatingLayout layout, CancellationToken cancellationToken);
}
