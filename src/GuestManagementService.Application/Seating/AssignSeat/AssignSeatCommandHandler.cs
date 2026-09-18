using GuestManagementService.Application.Abstractions.Common;
using GuestManagementService.Application.Abstractions.EventReferences;
using GuestManagementService.Application.Abstractions.Guests;
using GuestManagementService.Application.Abstractions.Seating;
using GuestManagementService.Application.Guests;
using GuestManagementService.Domain.Seating;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GuestManagementService.Application.Seating.AssignSeat;

public sealed class AssignSeatCommandHandler(
    IEventReferenceRepository eventReferenceRepository,
    ISeatingLayoutProvisioner seatingLayoutProvisioner,
    IGuestRepository guestRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<AssignSeatCommandHandler> logger)
    : IRequestHandler<AssignSeatCommand, AssignSeatResult>
{
    public async Task<AssignSeatResult> Handle(AssignSeatCommand request, CancellationToken cancellationToken)
    {
        var currentUser = request.CurrentUser;

        var eventReference = await eventReferenceRepository.GetByIdAsync(request.EventId, cancellationToken);

        if (eventReference is null || eventReference.IsDeleted || eventReference.TenantId != currentUser.TenantId)
        {
            logger.LogWarning(
                "Seat assignment requested but event reference was not available. EventId: {EventId}.",
                request.EventId);
            return AssignSeatResult.EventNotFound();
        }

        var layout = await seatingLayoutProvisioner.GetOrCreateAsync(request.EventId, currentUser.TenantId, cancellationToken);

        var table = layout.FindTable(request.TableId);
        if (table is null)
        {
            logger.LogWarning(
                "Seat assignment requested but table was not found. EventId: {EventId}. TableId: {TableId}.",
                request.EventId,
                request.TableId);
            return AssignSeatResult.TableNotFound();
        }

        if (request.SeatIndex >= table.SeatCount)
        {
            logger.LogWarning(
                "Seat assignment requested but seat index is out of range. EventId: {EventId}. TableId: {TableId}. SeatIndex: {SeatIndex}.",
                request.EventId,
                request.TableId,
                request.SeatIndex);
            return AssignSeatResult.SeatIndexOutOfRange();
        }

        var guest = await guestRepository.GetByIdAsync(request.EventId, request.GuestId, cancellationToken);
        if (guest is null)
        {
            logger.LogWarning(
                "Seat assignment requested but guest was not found for this event. EventId: {EventId}. GuestId: {GuestId}.",
                request.EventId,
                request.GuestId);
            return AssignSeatResult.GuestNotFound();
        }

        var now = timeProvider.GetUtcNow();
        var accompanyingGuestCount = GuestPartySize.AccompanyingGuestCount(guest);
        var outcome = layout.AssignGuestWithParty(
            request.TableId, request.SeatIndex, request.GuestId, accompanyingGuestCount, now, out _);

        if (outcome == SeatAssignmentOutcome.SeatOccupied)
        {
            logger.LogWarning(
                "Seat assignment rejected because the seat is occupied. EventId: {EventId}. TableId: {TableId}. SeatIndex: {SeatIndex}.",
                request.EventId,
                request.TableId,
                request.SeatIndex);
            return AssignSeatResult.SeatOccupied();
        }

        if (outcome == SeatAssignmentOutcome.InsufficientAdjacentSeats)
        {
            logger.LogWarning(
                "Seat assignment rejected because there weren't enough adjacent free seats for the guest's party. " +
                "EventId: {EventId}. TableId: {TableId}. SeatIndex: {SeatIndex}. AccompanyingGuestCount: {AccompanyingGuestCount}.",
                request.EventId,
                request.TableId,
                request.SeatIndex,
                accompanyingGuestCount);
            return AssignSeatResult.InsufficientAdjacentSeats();
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Seat assigned. EventId: {EventId}. TableId: {TableId}. SeatIndex: {SeatIndex}.",
            request.EventId,
            request.TableId,
            request.SeatIndex);

        return AssignSeatResult.Assigned(SeatingTableDetails.From(table, layout.Assignments));
    }
}
