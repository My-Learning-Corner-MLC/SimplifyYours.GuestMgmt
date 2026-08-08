namespace GuestManagementService.Application.Seating.ApplyAssignmentsBatch;

// Desired end-state per guest: Assign moves/places them at (TableId, SeatIndex); Unassign
// drops any seat they hold. Replaying the same op twice is a no-op, which is what lets the
// frontend coalesce and safely retry a debounced batch.
public sealed record SeatingBatchOpInput(
    SeatingBatchOpType Op,
    Guid GuestId,
    Guid? TableId,
    int? SeatIndex);
