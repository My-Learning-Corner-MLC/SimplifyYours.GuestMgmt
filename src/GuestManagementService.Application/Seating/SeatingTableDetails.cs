using GuestManagementService.Domain.Seating;

namespace GuestManagementService.Application.Seating;

public sealed record SeatingTableDetails(
    Guid Id,
    string Name,
    string Shape,
    int SeatCount,
    bool IsFull,
    double? PositionX,
    double? PositionY,
    double Rotation)
{
    public static SeatingTableDetails From(SeatingTable table)
    {
        return new SeatingTableDetails(
            table.Id,
            table.Name,
            table.Shape.ToString(),
            table.SeatCount,
            table.IsFull,
            table.PositionX,
            table.PositionY,
            table.Rotation);
    }
}
