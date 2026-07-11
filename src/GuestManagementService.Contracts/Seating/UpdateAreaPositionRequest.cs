namespace GuestManagementService.Contracts.Seating;

public sealed record UpdateAreaPositionRequest(
    Guid EventId,
    double PositionX,
    double PositionY,
    double Rotation);
