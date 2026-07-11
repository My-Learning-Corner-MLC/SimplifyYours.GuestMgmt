using GuestManagementService.Domain.Seating;

namespace GuestManagementService.Application.Seating;

public sealed record SeatingAreaDetails(
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
    int? Capacity)
{
    public static SeatingAreaDetails From(FloorPlanArea area)
    {
        return new SeatingAreaDetails(
            area.Id,
            area.Name,
            area.Kind.ToString(),
            area.Shape.ToString(),
            area.Width,
            area.Height,
            area.PositionX,
            area.PositionY,
            area.Rotation,
            area.Color,
            area.Capacity);
    }
}
