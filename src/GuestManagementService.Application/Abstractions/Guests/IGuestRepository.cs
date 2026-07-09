using GuestManagementService.Domain.Guests;

namespace GuestManagementService.Application.Abstractions.Guests;

public interface IGuestRepository
{
    Task AddAsync(Guest guest, CancellationToken cancellationToken);

    Task<IReadOnlyList<Guest>> ListByEventAsync(
        Guid eventId,
        CancellationToken cancellationToken);

    Task<bool> ExistsByPhoneAsync(
        Guid eventId,
        string normalizedPhoneNumber,
        CancellationToken cancellationToken);

    Task<bool> ExistsByEmailAsync(
        Guid eventId,
        string normalizedEmailAddress,
        CancellationToken cancellationToken);
}
