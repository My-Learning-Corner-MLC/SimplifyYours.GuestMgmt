using GuestManagementService.Domain.Invitations;

namespace GuestManagementService.Application.Abstractions.Invitations;

public interface IEventInvitationSettingsRepository
{
    /// <summary>Tenant-scoped: another tenant's settings are not found, not forbidden.</summary>
    Task<EventInvitationSettings?> GetByEventIdAsync(
        Guid eventId,
        Guid tenantId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Unscoped lookup for the anonymous render path, where there is no authenticated tenant —
    /// the caller has already resolved the event from an invitation token.
    /// </summary>
    Task<EventInvitationSettings?> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken);

    /// <summary>
    /// Resolves the public event link. Unscoped, like the lookup above — the token itself is the
    /// only credential an anonymous caller holds.
    /// </summary>
    Task<EventInvitationSettings?> GetByPublicEventTokenAsync(string token, CancellationToken cancellationToken);

    Task AddAsync(EventInvitationSettings settings, CancellationToken cancellationToken);

    Task UpdateAsync(EventInvitationSettings settings, CancellationToken cancellationToken);
}
