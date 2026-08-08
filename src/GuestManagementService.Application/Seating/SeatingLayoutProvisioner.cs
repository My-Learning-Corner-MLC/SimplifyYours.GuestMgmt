using GuestManagementService.Application.Abstractions.Common;
using GuestManagementService.Application.Abstractions.Seating;
using GuestManagementService.Domain.Seating;
using Microsoft.Extensions.Logging;

namespace GuestManagementService.Application.Seating;

internal sealed class SeatingLayoutProvisioner(
    ISeatingLayoutRepository repository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<SeatingLayoutProvisioner> logger)
    : ISeatingLayoutProvisioner
{
    public async Task<SeatingLayout> GetOrCreateAsync(
        Guid eventId,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var layout = await repository.GetByEventAsync(eventId, tenantId, cancellationToken);
        if (layout is not null)
        {
            return layout;
        }

        layout = SeatingLayout.Create(Guid.NewGuid(), eventId, tenantId, timeProvider.GetUtcNow());
        await repository.AddAsync(layout, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Seating layout created. SeatingLayoutId: {SeatingLayoutId}. EventId: {EventId}.",
            layout.Id,
            eventId);

        return layout;
    }
}
