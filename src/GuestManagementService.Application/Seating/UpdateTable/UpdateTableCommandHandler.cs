using GuestManagementService.Application.Abstractions.Common;
using GuestManagementService.Application.Abstractions.EventReferences;
using GuestManagementService.Application.Abstractions.Seating;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GuestManagementService.Application.Seating.UpdateTable;

public sealed class UpdateTableCommandHandler(
    IEventReferenceRepository eventReferenceRepository,
    ISeatingLayoutProvisioner seatingLayoutProvisioner,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<UpdateTableCommandHandler> logger)
    : IRequestHandler<UpdateTableCommand, UpdateTableResult>
{
    public async Task<UpdateTableResult> Handle(UpdateTableCommand request, CancellationToken cancellationToken)
    {
        var currentUser = request.CurrentUser;

        var eventReference = await eventReferenceRepository.GetByIdAsync(request.EventId, cancellationToken);

        if (eventReference is null || eventReference.IsDeleted || eventReference.TenantId != currentUser.TenantId)
        {
            logger.LogWarning(
                "Table update requested but event reference was not available. EventId: {EventId}.",
                request.EventId);
            return UpdateTableResult.EventNotFound();
        }

        var layout = await seatingLayoutProvisioner.GetOrCreateAsync(request.EventId, currentUser.TenantId, cancellationToken);

        var table = layout.FindTable(request.TableId);
        if (table is null)
        {
            logger.LogWarning(
                "Table update requested but table was not found. EventId: {EventId}. TableId: {TableId}.",
                request.EventId,
                request.TableId);
            return UpdateTableResult.TableNotFound();
        }

        var highestOccupiedSeatIndex = layout.Assignments
            .Where(assignment => assignment.SeatingTableId == table.Id)
            .Select(assignment => (int?)assignment.SeatIndex)
            .Max();

        if (highestOccupiedSeatIndex.HasValue && request.SeatCount <= highestOccupiedSeatIndex.Value)
        {
            logger.LogWarning(
                "Table update rejected because the new seat count would drop below an occupied seat. " +
                "EventId: {EventId}. TableId: {TableId}. RequestedSeatCount: {RequestedSeatCount}. HighestOccupiedSeatIndex: {HighestOccupiedSeatIndex}.",
                request.EventId,
                request.TableId,
                request.SeatCount,
                highestOccupiedSeatIndex.Value);
            return UpdateTableResult.SeatCountBelowOccupiedSeats();
        }

        SeatingParsing.TryParseShape(request.Shape, out var shape);
        var now = timeProvider.GetUtcNow();

        table.Rename(request.Name!.Trim(), now);
        table.Reshape(shape, request.SeatCount, now);
        table.SetFull(request.IsFull, now);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Table updated. EventId: {EventId}. TableId: {TableId}.",
            request.EventId,
            request.TableId);

        return UpdateTableResult.Updated(SeatingTableDetails.From(table, layout.Assignments));
    }
}
