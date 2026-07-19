using GuestManagementService.Application.Abstractions.Common;
using GuestManagementService.Application.Abstractions.EventReferences;
using GuestManagementService.Application.Abstractions.Seating;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GuestManagementService.Application.Seating.UpdateArea;

public sealed class UpdateAreaCommandHandler(
    IEventReferenceRepository eventReferenceRepository,
    ISeatingLayoutProvisioner seatingLayoutProvisioner,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<UpdateAreaCommandHandler> logger)
    : IRequestHandler<UpdateAreaCommand, UpdateAreaResult>
{
    public async Task<UpdateAreaResult> Handle(UpdateAreaCommand request, CancellationToken cancellationToken)
    {
        var currentUser = request.CurrentUser;

        var eventReference = await eventReferenceRepository.GetByIdAsync(request.EventId, cancellationToken);

        if (eventReference is null || eventReference.IsDeleted || eventReference.TenantId != currentUser.TenantId)
        {
            logger.LogWarning(
                "Area update requested but event reference was not available. EventId: {EventId}.",
                request.EventId);
            return UpdateAreaResult.EventNotFound();
        }

        var layout = await seatingLayoutProvisioner.GetOrCreateAsync(request.EventId, currentUser.TenantId, cancellationToken);

        var area = layout.FindArea(request.AreaId);
        if (area is null)
        {
            logger.LogWarning(
                "Area update requested but area was not found. EventId: {EventId}. AreaId: {AreaId}.",
                request.EventId,
                request.AreaId);
            return UpdateAreaResult.AreaNotFound();
        }

        SeatingParsing.TryParseAreaKind(request.Kind, out var kind);
        SeatingParsing.TryParseAreaShape(request.Shape, out var shape);

        area.Update(
            request.Name!.Trim(),
            kind,
            shape,
            request.Width,
            request.Height,
            request.Color,
            request.Capacity,
            timeProvider.GetUtcNow());

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Area updated. EventId: {EventId}. AreaId: {AreaId}.",
            request.EventId,
            request.AreaId);

        return UpdateAreaResult.Updated(SeatingAreaDetails.From(area));
    }
}
