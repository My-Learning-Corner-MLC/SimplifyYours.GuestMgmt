namespace GuestManagementService.Contracts.Seating;

public sealed record AreaPositionRequest(
    Guid AreaId,
    double PositionX,
    double PositionY,
    double Rotation);
