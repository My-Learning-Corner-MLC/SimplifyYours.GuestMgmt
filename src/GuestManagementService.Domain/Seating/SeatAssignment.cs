namespace GuestManagementService.Domain.Seating;

public sealed class SeatAssignment
{
    private SeatAssignment()
    {
    }

    private SeatAssignment(
        Guid id,
        Guid seatingTableId,
        Guid? guestId,
        Guid partyOwnerGuestId,
        int seatIndex,
        DateTimeOffset createdAt)
    {
        Id = id;
        SeatingTableId = seatingTableId;
        GuestId = guestId;
        PartyOwnerGuestId = partyOwnerGuestId;
        SeatIndex = seatIndex;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid SeatingTableId { get; private set; }

    // Null for a seat reserved for an accompanying attendee — those have no guest
    // record of their own (see CreateReservedForParty). Non-null for the seat the
    // named guest actually sits in.
    public Guid? GuestId { get; private set; }

    // The named guest this seat belongs to either way: for their own seat,
    // PartyOwnerGuestId == GuestId; for a seat reserved for their party,
    // GuestId is null and this is the only link back to who it's held for.
    public Guid PartyOwnerGuestId { get; private set; }

    public int SeatIndex { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public bool IsReservedForParty => GuestId is null;

    public static SeatAssignment Create(
        Guid id,
        Guid seatingTableId,
        Guid guestId,
        int seatIndex,
        DateTimeOffset createdAt)
    {
        ValidateCommon(id, seatingTableId, guestId, seatIndex);

        return new SeatAssignment(id, seatingTableId, guestId, guestId, seatIndex, createdAt.ToUniversalTime());
    }

    // An anonymous seat reserved for one of partyOwnerGuestId's accompanying
    // attendees — no name, no separate guest record, no RSVP tracking.
    public static SeatAssignment CreateReservedForParty(
        Guid id,
        Guid seatingTableId,
        Guid partyOwnerGuestId,
        int seatIndex,
        DateTimeOffset createdAt)
    {
        ValidateCommon(id, seatingTableId, partyOwnerGuestId, seatIndex);

        return new SeatAssignment(id, seatingTableId, null, partyOwnerGuestId, seatIndex, createdAt.ToUniversalTime());
    }

    private static void ValidateCommon(Guid id, Guid seatingTableId, Guid partyOwnerGuestId, int seatIndex)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Seat assignment id must not be empty.", nameof(id));
        }

        if (seatingTableId == Guid.Empty)
        {
            throw new ArgumentException("Seating table id must not be empty.", nameof(seatingTableId));
        }

        if (partyOwnerGuestId == Guid.Empty)
        {
            throw new ArgumentException("Party owner guest id must not be empty.", nameof(partyOwnerGuestId));
        }

        if (seatIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(seatIndex), seatIndex, "Seat index must not be negative.");
        }
    }
}
