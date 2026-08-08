using GuestManagementService.Application.Abstractions.Common;
using GuestManagementService.Application.Abstractions.EventReferences;
using GuestManagementService.Application.Abstractions.Seating;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GuestManagementService.Application.Seating.UnassignSeat;

public sealed class UnassignSeatCommandHandler(
    IEventReferenceRepository eventReferenceRepository,
    ISeatingLayoutProvisioner seatingLayoutProvisioner,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<UnassignSeatCommandHandler> logger)
    : IRequestHandler<UnassignSeatCommand, UnassignSeatResult>
{
    public async Task<UnassignSeatResult> Handle(UnassignSeatCommand request, CancellationToken cancellationToken)
    {
        var currentUser = request.CurrentUser;

        var eventReference = await eventReferenceRepository.GetByIdAsync(request.EventId, cancellationToken);

        if (eventReference is null || eventReference.IsDeleted || eventReference.TenantId != currentUser.TenantId)
        {
            logger.LogWarning(
                "Seat unassignment requested but event reference was not available. EventId: {EventId}.",
                request.EventId);
            return UnassignSeatResult.EventNotFound();
        }

        var layout = await seatingLayoutProvisioner.GetOrCreateAsync(request.EventId, currentUser.TenantId, cancellationToken);

        var table = layout.FindTable(request.TableId);
        if (table is null)
        {
            logger.LogWarning(
                "Seat unassignment requested but table was not found. EventId: {EventId}. TableId: {TableId}.",
                request.EventId,
                request.TableId);
            return UnassignSeatResult.TableNotFound();
        }

        // Unseating an already-empty seat is a no-op success (idempotent DELETE).
        var removed = layout.UnassignSeat(request.TableId, request.SeatIndex, timeProvider.GetUtcNow());
        if (removed)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            logger.LogInformation(
                "Seat unassigned. EventId: {EventId}. TableId: {TableId}. SeatIndex: {SeatIndex}.",
                request.EventId,
                request.TableId,
                request.SeatIndex);
        }

        return UnassignSeatResult.Unassigned();
    }
}
