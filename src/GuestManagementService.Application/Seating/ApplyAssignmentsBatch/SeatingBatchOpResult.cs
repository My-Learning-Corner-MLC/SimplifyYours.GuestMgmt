namespace GuestManagementService.Application.Seating.ApplyAssignmentsBatch;

public sealed record SeatingBatchOpResult(Guid GuestId, SeatingBatchOpStatus Status);
