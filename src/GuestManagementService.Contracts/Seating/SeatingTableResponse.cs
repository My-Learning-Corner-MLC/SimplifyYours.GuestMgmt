namespace GuestManagementService.Contracts.Seating;

public sealed record SeatingTableResponse(
    Guid Id,
    string Name,
    string Shape,
    int SeatCount,
    bool IsFull,
    double? PositionX,
    double? PositionY,
    double Rotation,
    IReadOnlyList<SeatingSeatResponse> Seats);
