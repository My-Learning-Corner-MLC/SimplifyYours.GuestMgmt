namespace GuestManagementService.Application.Seating.ApplyTablePositionsBatch;

public sealed record TablePositionInput(
    Guid TableId,
    double PositionX,
    double PositionY,
    double Rotation);
