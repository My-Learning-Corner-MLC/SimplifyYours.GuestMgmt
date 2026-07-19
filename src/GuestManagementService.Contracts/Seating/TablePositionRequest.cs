namespace GuestManagementService.Contracts.Seating;

public sealed record TablePositionRequest(
    Guid TableId,
    double PositionX,
    double PositionY,
    double Rotation);
