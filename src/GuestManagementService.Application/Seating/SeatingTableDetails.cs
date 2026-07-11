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
    double Rotation,
    IReadOnlyList<SeatingSeatDetails> Seats)
{
    public static SeatingTableDetails From(
        SeatingTable table,
        IReadOnlyCollection<SeatAssignment>? assignments = null,
        IReadOnlyDictionary<Guid, string>? guestNamesById = null)
    {
        var tableAssignments = assignments?.Where(a => a.SeatingTableId == table.Id).ToList()
            ?? [];

        var seats = Enumerable.Range(0, table.SeatCount)
            .Select(seatIndex =>
            {
                var assignment = tableAssignments.FirstOrDefault(a => a.SeatIndex == seatIndex);
                if (assignment is null)
                {
                    return SeatingSeatDetails.Empty(seatIndex);
                }

                var guestName = guestNamesById?.GetValueOrDefault(assignment.GuestId);
                return new SeatingSeatDetails(seatIndex, assignment.GuestId, guestName);
            })
            .ToList();

        return new SeatingTableDetails(
            table.Id,
            table.Name,
            table.Shape.ToString(),
            table.SeatCount,
            table.IsFull,
            table.PositionX,
            table.PositionY,
            table.Rotation,
            seats);
    }
}
