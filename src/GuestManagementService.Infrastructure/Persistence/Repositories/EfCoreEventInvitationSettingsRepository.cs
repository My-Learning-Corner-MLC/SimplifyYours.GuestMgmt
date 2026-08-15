using GuestManagementService.Application.Abstractions.Invitations;
using GuestManagementService.Domain.Invitations;
using Microsoft.EntityFrameworkCore;

namespace GuestManagementService.Infrastructure.Persistence.Repositories;

internal sealed class EfCoreEventInvitationSettingsRepository(GuestManagementServiceDbContext dbContext)
    : IEventInvitationSettingsRepository
{
    public async Task<EventInvitationSettings?> GetByEventIdAsync(
        Guid eventId,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        return await dbContext.EventInvitationSettings
            .SingleOrDefaultAsync(
                settings => settings.EventId == eventId && settings.TenantId == tenantId,
                cancellationToken);
    }

    public async Task<EventInvitationSettings?> GetByEventIdAsync(
        Guid eventId,
        CancellationToken cancellationToken)
    {
        return await dbContext.EventInvitationSettings
            .AsNoTracking()
            .SingleOrDefaultAsync(settings => settings.EventId == eventId, cancellationToken);
    }

    public async Task<EventInvitationSettings?> GetByPublicEventTokenAsync(
        string token,
        CancellationToken cancellationToken)
    {
        return await dbContext.EventInvitationSettings
            .AsNoTracking()
            .SingleOrDefaultAsync(settings => settings.PublicEventToken == token, cancellationToken);
    }

    public async Task<EventInvitationSettings?> GetByPreviewTokenAsync(
        string token,
        CancellationToken cancellationToken)
    {
        return await dbContext.EventInvitationSettings
            .AsNoTracking()
            .SingleOrDefaultAsync(settings => settings.PreviewToken == token, cancellationToken);
    }

    public async Task AddAsync(EventInvitationSettings settings, CancellationToken cancellationToken)
    {
        await dbContext.EventInvitationSettings.AddAsync(settings, cancellationToken);
    }

    public Task UpdateAsync(EventInvitationSettings settings, CancellationToken cancellationToken)
    {
        dbContext.EventInvitationSettings.Update(settings);

        return Task.CompletedTask;
    }
}
