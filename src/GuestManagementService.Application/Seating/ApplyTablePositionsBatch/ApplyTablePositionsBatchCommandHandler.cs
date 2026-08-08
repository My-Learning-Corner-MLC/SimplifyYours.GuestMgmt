using GuestManagementService.Application.Abstractions.Common;
using GuestManagementService.Application.Abstractions.EventReferences;
using GuestManagementService.Application.Abstractions.Seating;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GuestManagementService.Application.Seating.ApplyTablePositionsBatch;

public sealed class ApplyTablePositionsBatchCommandHandler(
    IEventReferenceRepository eventReferenceRepository,
    ISeatingLayoutProvisioner seatingLayoutProvisioner,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<ApplyTablePositionsBatchCommandHandler> logger)
    : IRequestHandler<ApplyTablePositionsBatchCommand, ApplyTablePositionsBatchResult>
{
    public async Task<ApplyTablePositionsBatchResult> Handle(
        ApplyTablePositionsBatchCommand request,
        CancellationToken cancellationToken)
    {
        var currentUser = request.CurrentUser;

        var eventReference = await eventReferenceRepository.GetByIdAsync(request.EventId, cancellationToken);

        if (eventReference is null || eventReference.IsDeleted || eventReference.TenantId != currentUser.TenantId)
        {
            logger.LogWarning(
                "Table position batch requested but event reference was not available. EventId: {EventId}.",
                request.EventId);
            return ApplyTablePositionsBatchResult.EventNotFound();
        }

        var layout = await seatingLayoutProvisioner.GetOrCreateAsync(request.EventId, currentUser.TenantId, cancellationToken);
        var now = timeProvider.GetUtcNow();

        var results = request.Positions
            .Select(position =>
            {
                var table = layout.FindTable(position.TableId);
                if (table is null)
                {
                    return new TablePositionOpResult(position.TableId, TablePositionOpStatus.TableNotFound);
                }

                table.Move(position.PositionX, position.PositionY, position.Rotation, now);
                return new TablePositionOpResult(position.TableId, TablePositionOpStatus.Applied);
            })
            .ToList();

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Table position batch applied. EventId: {EventId}. PositionCount: {PositionCount}.",
            request.EventId,
            request.Positions.Count);

        return ApplyTablePositionsBatchResult.Applied(results);
    }
}
