using GuestManagementService.Domain.Guests;

namespace GuestManagementService.Application.Abstractions.Guests;

public interface IGuestRepository
{
    Task AddAsync(Guest guest, CancellationToken cancellationToken);

    /// <summary>Tenant-scoped lookup: a guest owned by another tenant returns null.</summary>
    Task<Guest?> GetByIdAsync(Guid guestId, Guid tenantId, CancellationToken cancellationToken);

    /// <summary>
    /// Resolves a guest from their invitation token. Not tenant-scoped: this serves the anonymous
    /// public page, where the token itself is the only credential.
    /// </summary>
    Task<Guest?> GetByInvitationTokenAsync(string invitationToken, CancellationToken cancellationToken);

    Task UpdateAsync(Guest guest, CancellationToken cancellationToken);

    Task<GuestListPage> ListAsync(GuestListQueryOptions options, CancellationToken cancellationToken);

    Task<bool> ExistsByPhoneAsync(
        Guid eventId,
        string normalizedPhoneNumber,
        CancellationToken cancellationToken);

    Task<bool> ExistsByEmailAsync(
        Guid eventId,
        string normalizedEmailAddress,
        CancellationToken cancellationToken);
}
