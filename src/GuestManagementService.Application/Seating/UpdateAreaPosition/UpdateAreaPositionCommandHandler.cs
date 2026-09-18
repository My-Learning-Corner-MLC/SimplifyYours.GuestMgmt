using GuestManagementService.Application.Abstractions.Common;
using GuestManagementService.Application.Abstractions.EventReferences;
using GuestManagementService.Application.Abstractions.Seating;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GuestManagementService.Application.Seating.UpdateAreaPosition;

public sealed class UpdateAreaPositionCommandHandler(
    IEventReferenceRepository eventReferenceRepository,
    ISeatingLayoutProvisioner seatingLayoutProvisioner,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<UpdateAreaPositionCommandHandler> logger)
    : IRequestHandler<UpdateAreaPositionCommand, UpdateAreaPositionResult>
{
    public async Task<UpdateAreaPositionResult> Handle(UpdateAreaPositionCommand request, CancellationToken cancellationToken)
    {
        var currentUser = request.CurrentUser;

        var eventReference = await eventReferenceRepository.GetByIdAsync(request.EventId, cancellationToken);

        if (eventReference is null || eventReference.IsDeleted || eventReference.TenantId != currentUser.TenantId)
        {
            logger.LogWarning(
                "Area move requested but event reference was not available. EventId: {EventId}.",
                request.EventId);
            return UpdateAreaPositionResult.EventNotFound();
        }

        var layout = await seatingLayoutProvisioner.GetOrCreateAsync(request.EventId, currentUser.TenantId, cancellationToken);

        var area = layout.FindArea(request.AreaId);
        if (area is null)
        {
            logger.LogWarning(
                "Area move requested but area was not found. EventId: {EventId}. AreaId: {AreaId}.",
                request.EventId,
                request.AreaId);
            return UpdateAreaPositionResult.AreaNotFound();
        }

        area.Move(request.PositionX, request.PositionY, request.Rotation, timeProvider.GetUtcNow());

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return UpdateAreaPositionResult.Updated(SeatingAreaDetails.From(area));
    }
}
