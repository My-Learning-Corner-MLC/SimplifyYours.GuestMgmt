namespace GuestManagementService.Domain.Seating;

public sealed class SeatAssignment
{
    private SeatAssignment()
    {
    }

    private SeatAssignment(Guid id, Guid seatingTableId, Guid guestId, int seatIndex, DateTimeOffset createdAt)
    {
        Id = id;
        SeatingTableId = seatingTableId;
        GuestId = guestId;
        SeatIndex = seatIndex;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid SeatingTableId { get; private set; }

    public Guid GuestId { get; private set; }

    public int SeatIndex { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static SeatAssignment Create(
        Guid id,
        Guid seatingTableId,
        Guid guestId,
        int seatIndex,
        DateTimeOffset createdAt)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Seat assignment id must not be empty.", nameof(id));
        }

        if (seatingTableId == Guid.Empty)
        {
            throw new ArgumentException("Seating table id must not be empty.", nameof(seatingTableId));
        }

        if (guestId == Guid.Empty)
        {
            throw new ArgumentException("Guest id must not be empty.", nameof(guestId));
        }

        if (seatIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(seatIndex), seatIndex, "Seat index must not be negative.");
        }

        return new SeatAssignment(id, seatingTableId, guestId, seatIndex, createdAt.ToUniversalTime());
    }
}
