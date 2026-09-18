namespace GuestManagementService.Contracts.Seating;

public sealed record SeatingAreaResponse(
    Guid Id,
    string Name,
    string Kind,
    string Shape,
    double Width,
    double Height,
    double? PositionX,
    double? PositionY,
    double Rotation,
    string? Color,
    int? Capacity);
