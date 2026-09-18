namespace GuestManagementService.Contracts.Seating;

public sealed record UpdateTablePositionRequest(
    Guid EventId,
    double PositionX,
    double PositionY,
    double Rotation);
