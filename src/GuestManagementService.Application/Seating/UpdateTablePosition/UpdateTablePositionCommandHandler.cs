using GuestManagementService.Application.Abstractions.Common;
using GuestManagementService.Application.Abstractions.EventReferences;
using GuestManagementService.Application.Abstractions.Seating;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GuestManagementService.Application.Seating.UpdateTablePosition;

public sealed class UpdateTablePositionCommandHandler(
    IEventReferenceRepository eventReferenceRepository,
    ISeatingLayoutProvisioner seatingLayoutProvisioner,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<UpdateTablePositionCommandHandler> logger)
    : IRequestHandler<UpdateTablePositionCommand, UpdateTablePositionResult>
{
    public async Task<UpdateTablePositionResult> Handle(UpdateTablePositionCommand request, CancellationToken cancellationToken)
    {
        var currentUser = request.CurrentUser;

        var eventReference = await eventReferenceRepository.GetByIdAsync(request.EventId, cancellationToken);

        if (eventReference is null || eventReference.IsDeleted || eventReference.TenantId != currentUser.TenantId)
        {
            logger.LogWarning(
                "Table move requested but event reference was not available. EventId: {EventId}.",
                request.EventId);
            return UpdateTablePositionResult.EventNotFound();
        }

        var layout = await seatingLayoutProvisioner.GetOrCreateAsync(request.EventId, currentUser.TenantId, cancellationToken);

        var table = layout.FindTable(request.TableId);
        if (table is null)
        {
            logger.LogWarning(
                "Table move requested but table was not found. EventId: {EventId}. TableId: {TableId}.",
                request.EventId,
                request.TableId);
            return UpdateTablePositionResult.TableNotFound();
        }

        table.Move(request.PositionX, request.PositionY, request.Rotation, timeProvider.GetUtcNow());

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return UpdateTablePositionResult.Updated(SeatingTableDetails.From(table, layout.Assignments));
    }
}
