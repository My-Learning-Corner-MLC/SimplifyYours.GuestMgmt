namespace GuestManagementService.Application.Seating.ApplyAreaPositionsBatch;

public sealed record AreaPositionInput(
    Guid AreaId,
    double PositionX,
    double PositionY,
    double Rotation);
