using GuestManagementService.Application.Abstractions.Common;
using GuestManagementService.Application.Abstractions.EventReferences;
using GuestManagementService.Application.Abstractions.Seating;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GuestManagementService.Application.Seating.DeleteArea;

public sealed class DeleteAreaCommandHandler(
    IEventReferenceRepository eventReferenceRepository,
    ISeatingLayoutProvisioner seatingLayoutProvisioner,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<DeleteAreaCommandHandler> logger)
    : IRequestHandler<DeleteAreaCommand, DeleteAreaResult>
{
    public async Task<DeleteAreaResult> Handle(DeleteAreaCommand request, CancellationToken cancellationToken)
    {
        var currentUser = request.CurrentUser;

        var eventReference = await eventReferenceRepository.GetByIdAsync(request.EventId, cancellationToken);

        if (eventReference is null || eventReference.IsDeleted || eventReference.TenantId != currentUser.TenantId)
        {
            logger.LogWarning(
                "Area deletion requested but event reference was not available. EventId: {EventId}.",
                request.EventId);
            return DeleteAreaResult.EventNotFound();
        }

        var layout = await seatingLayoutProvisioner.GetOrCreateAsync(request.EventId, currentUser.TenantId, cancellationToken);

        var removed = layout.RemoveArea(request.AreaId, timeProvider.GetUtcNow());
        if (!removed)
        {
            logger.LogWarning(
                "Area deletion requested but area was not found. EventId: {EventId}. AreaId: {AreaId}.",
                request.EventId,
                request.AreaId);
            return DeleteAreaResult.AreaNotFound();
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Area deleted. EventId: {EventId}. AreaId: {AreaId}.",
            request.EventId,
            request.AreaId);

        return DeleteAreaResult.Deleted();
    }
}
