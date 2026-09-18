using GuestManagementService.Domain.Seating;

namespace GuestManagementService.UnitTests.Seating;

public sealed class SeatAssignmentTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 12, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_SetsFields()
    {
        var id = Guid.NewGuid();
        var tableId = Guid.NewGuid();
        var guestId = Guid.NewGuid();

        var assignment = SeatAssignment.Create(id, tableId, guestId, 3, Now);

        Assert.Equal(id, assignment.Id);
        Assert.Equal(tableId, assignment.SeatingTableId);
        Assert.Equal(guestId, assignment.GuestId);
        Assert.Equal(guestId, assignment.PartyOwnerGuestId);
        Assert.Equal(3, assignment.SeatIndex);
        Assert.Equal(Now, assignment.CreatedAt);
        Assert.False(assignment.IsReservedForParty);
    }

    [Fact]
    public void CreateReservedForParty_SetsNullGuestIdAndPartyOwner()
    {
        var id = Guid.NewGuid();
        var tableId = Guid.NewGuid();
        var partyOwnerGuestId = Guid.NewGuid();

        var assignment = SeatAssignment.CreateReservedForParty(id, tableId, partyOwnerGuestId, 4, Now);

        Assert.Equal(id, assignment.Id);
        Assert.Equal(tableId, assignment.SeatingTableId);
        Assert.Null(assignment.GuestId);
        Assert.Equal(partyOwnerGuestId, assignment.PartyOwnerGuestId);
        Assert.Equal(4, assignment.SeatIndex);
        Assert.True(assignment.IsReservedForParty);
    }

    [Fact]
    public void CreateReservedForParty_WhenPartyOwnerEmpty_Throws()
    {
        Assert.Throws<ArgumentException>(
            () => SeatAssignment.CreateReservedForParty(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, 0, Now));
    }

    [Fact]
    public void Create_WhenSeatIndexNegative_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => SeatAssignment.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), -1, Now));
    }

    [Fact]
    public void Create_WhenIdentifiersEmpty_Throws()
    {
        Assert.Throws<ArgumentException>(
            () => SeatAssignment.Create(Guid.NewGuid(), Guid.Empty, Guid.NewGuid(), 0, Now));

        Assert.Throws<ArgumentException>(
            () => SeatAssignment.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, 0, Now));
    }
}
