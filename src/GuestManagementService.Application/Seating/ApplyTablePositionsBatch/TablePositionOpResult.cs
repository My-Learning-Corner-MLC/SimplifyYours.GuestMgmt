namespace GuestManagementService.Application.Seating.ApplyTablePositionsBatch;

public sealed record TablePositionOpResult(Guid TableId, TablePositionOpStatus Status);
