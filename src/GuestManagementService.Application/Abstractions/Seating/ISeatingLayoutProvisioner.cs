using GuestManagementService.Domain.Seating;

namespace GuestManagementService.Application.Abstractions.Seating;

// Shared by every Seating mutation handler so "load the layout, creating it
// on first use" isn't reimplemented in each one (GetSeatingLayoutQueryHandler
// is the one exception - it owns the original inline version).
public interface ISeatingLayoutProvisioner
{
    Task<SeatingLayout> GetOrCreateAsync(
        Guid eventId,
        Guid tenantId,
        CancellationToken cancellationToken);
}
