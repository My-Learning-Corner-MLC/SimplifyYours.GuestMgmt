using GuestManagementService.Application.Abstractions.Common;
using GuestManagementService.Application.Abstractions.EventReferences;
using GuestManagementService.Application.Abstractions.Seating;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GuestManagementService.Application.Seating.ApplyAreaPositionsBatch;

public sealed class ApplyAreaPositionsBatchCommandHandler(
    IEventReferenceRepository eventReferenceRepository,
    ISeatingLayoutProvisioner seatingLayoutProvisioner,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<ApplyAreaPositionsBatchCommandHandler> logger)
    : IRequestHandler<ApplyAreaPositionsBatchCommand, ApplyAreaPositionsBatchResult>
{
    public async Task<ApplyAreaPositionsBatchResult> Handle(
        ApplyAreaPositionsBatchCommand request,
        CancellationToken cancellationToken)
    {
        var currentUser = request.CurrentUser;

        var eventReference = await eventReferenceRepository.GetByIdAsync(request.EventId, cancellationToken);

        if (eventReference is null || eventReference.IsDeleted || eventReference.TenantId != currentUser.TenantId)
        {
            logger.LogWarning(
                "Area position batch requested but event reference was not available. EventId: {EventId}.",
                request.EventId);
            return ApplyAreaPositionsBatchResult.EventNotFound();
        }

        var layout = await seatingLayoutProvisioner.GetOrCreateAsync(request.EventId, currentUser.TenantId, cancellationToken);
        var now = timeProvider.GetUtcNow();

        var results = request.Positions
            .Select(position =>
            {
                var area = layout.FindArea(position.AreaId);
                if (area is null)
                {
                    return new AreaPositionOpResult(position.AreaId, AreaPositionOpStatus.AreaNotFound);
                }

                area.Move(position.PositionX, position.PositionY, position.Rotation, now);
                return new AreaPositionOpResult(position.AreaId, AreaPositionOpStatus.Applied);
            })
            .ToList();

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Area position batch applied. EventId: {EventId}. PositionCount: {PositionCount}.",
            request.EventId,
            request.Positions.Count);

        return ApplyAreaPositionsBatchResult.Applied(results);
    }
}
