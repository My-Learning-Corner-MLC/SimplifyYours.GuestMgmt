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

    Task AddAsync(EventInvitationSettings settings, CancellationToken cancellationToken);

    Task UpdateAsync(EventInvitationSettings settings, CancellationToken cancellationToken);
}
