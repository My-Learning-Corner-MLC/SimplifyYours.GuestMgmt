using GuestManagementService.Application.Abstractions.Common;
using GuestManagementService.Application.Abstractions.EventReferences;
using GuestManagementService.Application.Abstractions.Seating;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GuestManagementService.Application.Seating.DeleteTable;

public sealed class DeleteTableCommandHandler(
    IEventReferenceRepository eventReferenceRepository,
    ISeatingLayoutProvisioner seatingLayoutProvisioner,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<DeleteTableCommandHandler> logger)
    : IRequestHandler<DeleteTableCommand, DeleteTableResult>
{
    public async Task<DeleteTableResult> Handle(DeleteTableCommand request, CancellationToken cancellationToken)
    {
        var currentUser = request.CurrentUser;

        var eventReference = await eventReferenceRepository.GetByIdAsync(request.EventId, cancellationToken);

        if (eventReference is null || eventReference.IsDeleted || eventReference.TenantId != currentUser.TenantId)
        {
            logger.LogWarning(
                "Table deletion requested but event reference was not available. EventId: {EventId}.",
                request.EventId);
            return DeleteTableResult.EventNotFound();
        }

        var layout = await seatingLayoutProvisioner.GetOrCreateAsync(request.EventId, currentUser.TenantId, cancellationToken);

        var removed = layout.RemoveTable(request.TableId, timeProvider.GetUtcNow());
        if (!removed)
        {
            logger.LogWarning(
                "Table deletion requested but table was not found. EventId: {EventId}. TableId: {TableId}.",
                request.EventId,
                request.TableId);
            return DeleteTableResult.TableNotFound();
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Table deleted. EventId: {EventId}. TableId: {TableId}.",
            request.EventId,
            request.TableId);

        return DeleteTableResult.Deleted();
    }
}
