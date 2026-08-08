using GuestManagementService.Application.Abstractions.Common;
using GuestManagementService.Application.Abstractions.EventReferences;
using GuestManagementService.Application.Abstractions.Guests;
using GuestManagementService.Application.Abstractions.Seating;
using GuestManagementService.Application.Guests;
using GuestManagementService.Application.Seating;
using GuestManagementService.Domain.Guests;
using GuestManagementService.Domain.Seating;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GuestManagementService.Application.Seating.ApplyAssignmentsBatch;

public sealed class ApplyAssignmentsBatchCommandHandler(
    IEventReferenceRepository eventReferenceRepository,
    ISeatingLayoutProvisioner seatingLayoutProvisioner,
    IGuestRepository guestRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<ApplyAssignmentsBatchCommandHandler> logger)
    : IRequestHandler<ApplyAssignmentsBatchCommand, ApplyAssignmentsBatchResult>
{
    public async Task<ApplyAssignmentsBatchResult> Handle(ApplyAssignmentsBatchCommand request, CancellationToken cancellationToken)
    {
        var currentUser = request.CurrentUser;

        var eventReference = await eventReferenceRepository.GetByIdAsync(request.EventId, cancellationToken);

        if (eventReference is null || eventReference.IsDeleted || eventReference.TenantId != currentUser.TenantId)
        {
            logger.LogWarning(
                "Seating batch requested but event reference was not available. EventId: {EventId}.",
                request.EventId);
            return ApplyAssignmentsBatchResult.EventNotFound();
        }

        var layout = await seatingLayoutProvisioner.GetOrCreateAsync(request.EventId, currentUser.TenantId, cancellationToken);
        var guests = await GuestRoster.LoadAllAsync(guestRepository, request.EventId, currentUser.TenantId, cancellationToken);
        var guestsById = guests.ToDictionary(guest => guest.Id);

        var now = timeProvider.GetUtcNow();
        var opResults = new List<SeatingBatchOpResult>(request.Ops.Count);

        foreach (var op in request.Ops)
        {
            opResults.Add(Apply(layout, guestsById, op, now));
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Seating batch applied. EventId: {EventId}. OpCount: {OpCount}. AppliedCount: {AppliedCount}.",
            request.EventId,
            request.Ops.Count,
            opResults.Count(result => result.Status == SeatingBatchOpStatus.Applied));

        var details = SeatingLayoutProjector.Project(layout, guests);
        return ApplyAssignmentsBatchResult.Applied(details, opResults);
    }

    private static SeatingBatchOpResult Apply(
        SeatingLayout layout,
        IReadOnlyDictionary<Guid, Guest> guestsById,
        SeatingBatchOpInput op,
        DateTimeOffset now)
    {
        if (op.Op == SeatingBatchOpType.Unassign)
        {
            // Desired end-state is "no seat" — a no-op when the guest is already unseated
            // still counts as applied, keeping the op idempotent and safe to replay.
            layout.UnassignGuest(op.GuestId, now);
            return new SeatingBatchOpResult(op.GuestId, SeatingBatchOpStatus.Applied);
        }

        if (!guestsById.TryGetValue(op.GuestId, out var guest))
        {
            return new SeatingBatchOpResult(op.GuestId, SeatingBatchOpStatus.GuestNotFound);
        }

        var table = layout.FindTable(op.TableId!.Value);
        if (table is null)
        {
            return new SeatingBatchOpResult(op.GuestId, SeatingBatchOpStatus.TableNotFound);
        }

        if (op.SeatIndex!.Value >= table.SeatCount)
        {
            return new SeatingBatchOpResult(op.GuestId, SeatingBatchOpStatus.SeatIndexOutOfRange);
        }

        var accompanyingGuestCount = GuestPartySize.AccompanyingGuestCount(guest);
        var outcome = layout.AssignGuestWithParty(
            table.Id, op.SeatIndex.Value, op.GuestId, accompanyingGuestCount, now, out _);
        var status = outcome switch
        {
            SeatAssignmentOutcome.Assigned => SeatingBatchOpStatus.Applied,
            SeatAssignmentOutcome.InsufficientAdjacentSeats => SeatingBatchOpStatus.InsufficientAdjacentSeats,
            _ => SeatingBatchOpStatus.Conflict,
        };
        return new SeatingBatchOpResult(op.GuestId, status);
    }
}
