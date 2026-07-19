using GuestManagementService.Domain.Seating;

namespace GuestManagementService.UnitTests.Seating;

public sealed class SeatingLayoutTests
{
    private static readonly DateTimeOffset Created = new(2026, 9, 12, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Later = new(2026, 9, 12, 11, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_SetsIdentityAndTimestamps()
    {
        var id = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var layout = SeatingLayout.Create(id, eventId, tenantId, Created);

        Assert.Equal(id, layout.Id);
        Assert.Equal(eventId, layout.EventId);
        Assert.Equal(tenantId, layout.TenantId);
        Assert.Equal(Created, layout.CreatedAt);
        Assert.Equal(Created, layout.UpdatedAt);
        Assert.Empty(layout.Tables);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void Create_WhenIdentifiersEmpty_Throws(bool emptyEvent, bool emptyTenant)
    {
        var eventId = emptyEvent ? Guid.Empty : Guid.NewGuid();
        var tenantId = emptyTenant ? Guid.Empty : Guid.NewGuid();

        Assert.Throws<ArgumentException>(
            () => SeatingLayout.Create(Guid.NewGuid(), eventId, tenantId, Created));
    }

    [Fact]
    public void AddTable_AppendsTableAndBumpsUpdatedAt()
    {
        var layout = SeatingLayout.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Created);
        var tableId = Guid.NewGuid();

        var table = layout.AddTable(tableId, "Family", TableShape.Round, 8, Later);

        Assert.Equal(tableId, table.Id);
        Assert.Equal(layout.Id, table.SeatingLayoutId);
        Assert.Contains(table, layout.Tables);
        Assert.Single(layout.Tables);
        Assert.Equal(Later, layout.UpdatedAt);
        Assert.Equal(Created, layout.CreatedAt);
    }

    [Fact]
    public void FindTable_WhenPresent_ReturnsTable()
    {
        var layout = SeatingLayout.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Created);
        var table = layout.AddTable(Guid.NewGuid(), "Family", TableShape.Round, 8, Later);

        var found = layout.FindTable(table.Id);

        Assert.Same(table, found);
    }

    [Fact]
    public void FindTable_WhenAbsent_ReturnsNull()
    {
        var layout = SeatingLayout.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Created);

        Assert.Null(layout.FindTable(Guid.NewGuid()));
    }

    [Fact]
    public void RemoveTable_WhenPresent_RemovesItAndBumpsUpdatedAt()
    {
        var layout = SeatingLayout.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Created);
        var table = layout.AddTable(Guid.NewGuid(), "Family", TableShape.Round, 8, Created);

        var removed = layout.RemoveTable(table.Id, Later);

        Assert.True(removed);
        Assert.Empty(layout.Tables);
        Assert.Equal(Later, layout.UpdatedAt);
    }

    [Fact]
    public void RemoveTable_WhenAbsent_ReturnsFalseAndDoesNotBumpUpdatedAt()
    {
        var layout = SeatingLayout.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Created);

        var removed = layout.RemoveTable(Guid.NewGuid(), Later);

        Assert.False(removed);
        Assert.Equal(Created, layout.UpdatedAt);
    }

    [Fact]
    public void AssignGuest_ToEmptySeat_Assigns()
    {
        var layout = SeatingLayout.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Created);
        var table = layout.AddTable(Guid.NewGuid(), "Family", TableShape.Round, 8, Created);
        var guestId = Guid.NewGuid();

        var outcome = layout.AssignGuest(table.Id, 2, guestId, Later, out var assignment);

        Assert.Equal(SeatAssignmentOutcome.Assigned, outcome);
        Assert.NotNull(assignment);
        Assert.Equal(table.Id, assignment.SeatingTableId);
        Assert.Equal(2, assignment.SeatIndex);
        Assert.Equal(guestId, assignment.GuestId);
        Assert.Contains(assignment, layout.Assignments);
        Assert.Equal(Later, layout.UpdatedAt);
    }

    [Fact]
    public void AssignGuest_WhenSeatOccupiedByAnotherGuest_ReturnsSeatOccupiedAndDoesNotMutate()
    {
        var layout = SeatingLayout.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Created);
        var table = layout.AddTable(Guid.NewGuid(), "Family", TableShape.Round, 8, Created);
        var firstGuest = Guid.NewGuid();
        var secondGuest = Guid.NewGuid();
        layout.AssignGuest(table.Id, 0, firstGuest, Created, out _);

        var outcome = layout.AssignGuest(table.Id, 0, secondGuest, Later, out var assignment);

        Assert.Equal(SeatAssignmentOutcome.SeatOccupied, outcome);
        Assert.Null(assignment);
        Assert.Single(layout.Assignments);
        Assert.Equal(firstGuest, layout.Assignments.Single().GuestId);
    }

    [Fact]
    public void AssignGuest_WhenGuestAlreadySeatedElsewhere_MovesThem()
    {
        var layout = SeatingLayout.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Created);
        var tableA = layout.AddTable(Guid.NewGuid(), "Family", TableShape.Round, 8, Created);
        var tableB = layout.AddTable(Guid.NewGuid(), "Friends", TableShape.Round, 8, Created);
        var guestId = Guid.NewGuid();
        layout.AssignGuest(tableA.Id, 0, guestId, Created, out _);

        var outcome = layout.AssignGuest(tableB.Id, 3, guestId, Later, out var assignment);

        Assert.Equal(SeatAssignmentOutcome.Assigned, outcome);
        Assert.NotNull(assignment);
        var only = Assert.Single(layout.Assignments);
        Assert.Equal(tableB.Id, only.SeatingTableId);
        Assert.Equal(3, only.SeatIndex);
    }

    [Fact]
    public void AssignGuest_WhenGuestHasAPartyAndMovesViaTheSingleSeatMethod_ReleasesTheirReservedSeatsToo()
    {
        // AssignGuest is a thin wrapper over AssignGuestWithParty(..., accompanyingGuestCount: 0, ...)
        // specifically so it can't strand a guest's party reservations on a move — this proves it.
        var layout = SeatingLayout.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Created);
        var table = layout.AddTable(Guid.NewGuid(), "Family", TableShape.Round, 8, Created);
        var guestId = Guid.NewGuid();
        layout.AssignGuestWithParty(table.Id, 0, guestId, 2, Created, out _);
        Assert.Equal(3, layout.Assignments.Count);

        var outcome = layout.AssignGuest(table.Id, 5, guestId, Later, out _);

        Assert.Equal(SeatAssignmentOutcome.Assigned, outcome);
        var only = Assert.Single(layout.Assignments);
        Assert.Equal(5, only.SeatIndex);
        Assert.False(only.IsReservedForParty);
    }

    [Fact]
    public void AssignGuest_WhenReassigningSameGuestToSameSeat_IsIdempotent()
    {
        var layout = SeatingLayout.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Created);
        var table = layout.AddTable(Guid.NewGuid(), "Family", TableShape.Round, 8, Created);
        var guestId = Guid.NewGuid();
        layout.AssignGuest(table.Id, 0, guestId, Created, out _);

        var outcome = layout.AssignGuest(table.Id, 0, guestId, Later, out var assignment);

        Assert.Equal(SeatAssignmentOutcome.Assigned, outcome);
        Assert.NotNull(assignment);
        Assert.Single(layout.Assignments);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(8)]
    public void AssignGuest_WhenSeatIndexOutOfTableRange_Throws(int seatIndex)
    {
        var layout = SeatingLayout.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Created);
        var table = layout.AddTable(Guid.NewGuid(), "Family", TableShape.Round, 8, Created);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => layout.AssignGuest(table.Id, seatIndex, Guid.NewGuid(), Later, out _));
    }

    [Fact]
    public void AssignGuest_WhenTableNotInLayout_Throws()
    {
        var layout = SeatingLayout.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Created);

        Assert.Throws<InvalidOperationException>(
            () => layout.AssignGuest(Guid.NewGuid(), 0, Guid.NewGuid(), Later, out _));
    }

    [Fact]
    public void AssignGuestWithParty_WithNoAccompanyingGuests_BehavesLikeAssignGuest()
    {
        var layout = SeatingLayout.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Created);
        var table = layout.AddTable(Guid.NewGuid(), "Family", TableShape.Round, 8, Created);
        var guestId = Guid.NewGuid();

        var outcome = layout.AssignGuestWithParty(table.Id, 2, guestId, 0, Later, out var assignments);

        Assert.Equal(SeatAssignmentOutcome.Assigned, outcome);
        var assignment = Assert.Single(assignments);
        Assert.Equal(2, assignment.SeatIndex);
        Assert.Equal(guestId, assignment.GuestId);
        Assert.False(assignment.IsReservedForParty);
    }

    [Fact]
    public void AssignGuestWithParty_ReservesAdjacentSeatsForAccompanyingGuests()
    {
        var layout = SeatingLayout.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Created);
        var table = layout.AddTable(Guid.NewGuid(), "Family", TableShape.Round, 8, Created);
        var guestId = Guid.NewGuid();

        var outcome = layout.AssignGuestWithParty(table.Id, 2, guestId, 2, Later, out var assignments);

        Assert.Equal(SeatAssignmentOutcome.Assigned, outcome);
        Assert.Equal(3, assignments.Count);
        Assert.Equal(3, layout.Assignments.Count);
        var primary = Assert.Single(assignments, a => !a.IsReservedForParty);
        Assert.Equal(2, primary.SeatIndex);
        Assert.Equal(guestId, primary.GuestId);
        var reserved = assignments.Where(a => a.IsReservedForParty).ToList();
        Assert.Equal(2, reserved.Count);
        Assert.All(reserved, a => Assert.Null(a.GuestId));
        Assert.All(reserved, a => Assert.Equal(guestId, a.PartyOwnerGuestId));
        Assert.All(reserved, a => Assert.Contains(a.SeatIndex, new[] { 1, 3 }));
    }

    [Fact]
    public void AssignGuestWithParty_WhenNotEnoughAdjacentSeats_ReturnsInsufficientAndDoesNotMutate()
    {
        var layout = SeatingLayout.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Created);
        var table = layout.AddTable(Guid.NewGuid(), "Family", TableShape.Round, 4, Created);
        var guestId = Guid.NewGuid();
        layout.AssignGuest(table.Id, 1, Guid.NewGuid(), Created, out _);
        layout.AssignGuest(table.Id, 3, Guid.NewGuid(), Created, out _);

        // Seat 0 is free but boxed in by occupied seats 1 and 3 on a 4-seat ring —
        // only 1 contiguous free seat is reachable, not enough for a party of 2.
        var outcome = layout.AssignGuestWithParty(table.Id, 0, guestId, 1, Later, out var assignments);

        Assert.Equal(SeatAssignmentOutcome.InsufficientAdjacentSeats, outcome);
        Assert.Empty(assignments);
        Assert.Equal(2, layout.Assignments.Count);
        Assert.DoesNotContain(layout.Assignments, a => a.PartyOwnerGuestId == guestId);
    }

    [Fact]
    public void AssignGuestWithParty_WhenAnchorSeatOccupiedByAnotherGuest_ReturnsSeatOccupied()
    {
        var layout = SeatingLayout.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Created);
        var table = layout.AddTable(Guid.NewGuid(), "Family", TableShape.Round, 8, Created);
        layout.AssignGuest(table.Id, 0, Guid.NewGuid(), Created, out _);

        var outcome = layout.AssignGuestWithParty(table.Id, 0, Guid.NewGuid(), 1, Later, out var assignments);

        Assert.Equal(SeatAssignmentOutcome.SeatOccupied, outcome);
        Assert.Empty(assignments);
        Assert.Single(layout.Assignments);
    }

    [Fact]
    public void AssignGuestWithParty_WhenGuestAlreadyHasAParty_MovesTheWholePartyTogether()
    {
        var layout = SeatingLayout.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Created);
        var tableA = layout.AddTable(Guid.NewGuid(), "Family", TableShape.Round, 8, Created);
        var tableB = layout.AddTable(Guid.NewGuid(), "Friends", TableShape.Round, 8, Created);
        var guestId = Guid.NewGuid();
        layout.AssignGuestWithParty(tableA.Id, 0, guestId, 2, Created, out _);
        Assert.Equal(3, layout.Assignments.Count);

        var outcome = layout.AssignGuestWithParty(tableB.Id, 4, guestId, 1, Later, out var assignments);

        Assert.Equal(SeatAssignmentOutcome.Assigned, outcome);
        Assert.Equal(2, assignments.Count);
        Assert.All(layout.Assignments, a => Assert.Equal(tableB.Id, a.SeatingTableId));
        Assert.Equal(2, layout.Assignments.Count);
    }

    [Fact]
    public void AssignGuestWithParty_WhenAccompanyingCountNegative_Throws()
    {
        var layout = SeatingLayout.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Created);
        var table = layout.AddTable(Guid.NewGuid(), "Family", TableShape.Round, 8, Created);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => layout.AssignGuestWithParty(table.Id, 0, Guid.NewGuid(), -1, Later, out _));
    }

    [Fact]
    public void UnassignGuest_AlsoReleasesSeatsReservedForTheirParty()
    {
        var layout = SeatingLayout.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Created);
        var table = layout.AddTable(Guid.NewGuid(), "Family", TableShape.Round, 8, Created);
        var guestId = Guid.NewGuid();
        layout.AssignGuestWithParty(table.Id, 0, guestId, 2, Created, out _);

        var removed = layout.UnassignGuest(guestId, Later);

        Assert.True(removed);
        Assert.Empty(layout.Assignments);
    }

    [Fact]
    public void UnassignSeat_OnAReservedSeat_ReleasesTheWholeParty()
    {
        var layout = SeatingLayout.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Created);
        var table = layout.AddTable(Guid.NewGuid(), "Family", TableShape.Round, 8, Created);
        var guestId = Guid.NewGuid();
        layout.AssignGuestWithParty(table.Id, 0, guestId, 2, Created, out _);

        // Seat 1 is a reserved (anonymous) seat, not the guest's own seat.
        var removed = layout.UnassignSeat(table.Id, 1, Later);

        Assert.True(removed);
        Assert.Empty(layout.Assignments);
    }

    [Fact]
    public void UnassignSeat_WhenOccupied_RemovesAssignment()
    {
        var layout = SeatingLayout.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Created);
        var table = layout.AddTable(Guid.NewGuid(), "Family", TableShape.Round, 8, Created);
        layout.AssignGuest(table.Id, 0, Guid.NewGuid(), Created, out _);

        var removed = layout.UnassignSeat(table.Id, 0, Later);

        Assert.True(removed);
        Assert.Empty(layout.Assignments);
        Assert.Equal(Later, layout.UpdatedAt);
    }

    [Fact]
    public void UnassignSeat_WhenEmpty_ReturnsFalseAndDoesNotBumpUpdatedAt()
    {
        var layout = SeatingLayout.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Created);
        var table = layout.AddTable(Guid.NewGuid(), "Family", TableShape.Round, 8, Created);

        var removed = layout.UnassignSeat(table.Id, 0, Later);

        Assert.False(removed);
        Assert.Equal(Created, layout.UpdatedAt);
    }

    [Fact]
    public void UnassignGuest_WhenSeated_RemovesTheirAssignment()
    {
        var layout = SeatingLayout.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Created);
        var table = layout.AddTable(Guid.NewGuid(), "Family", TableShape.Round, 8, Created);
        var guestId = Guid.NewGuid();
        layout.AssignGuest(table.Id, 0, guestId, Created, out _);

        var removed = layout.UnassignGuest(guestId, Later);

        Assert.True(removed);
        Assert.Empty(layout.Assignments);
    }

    [Fact]
    public void UnassignGuest_WhenNotSeated_ReturnsFalse()
    {
        var layout = SeatingLayout.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Created);

        Assert.False(layout.UnassignGuest(Guid.NewGuid(), Later));
    }

    [Fact]
    public void RemoveTable_AlsoRemovesItsAssignments()
    {
        var layout = SeatingLayout.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Created);
        var table = layout.AddTable(Guid.NewGuid(), "Family", TableShape.Round, 8, Created);
        layout.AssignGuest(table.Id, 0, Guid.NewGuid(), Created, out _);

        layout.RemoveTable(table.Id, Later);

        Assert.Empty(layout.Assignments);
    }

    [Fact]
    public void AddArea_AppendsAreaAndBumpsUpdatedAt()
    {
        var layout = SeatingLayout.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Created);
        var areaId = Guid.NewGuid();

        var area = layout.AddArea(areaId, "Stage", AreaKind.Stage, AreaShape.Rect, 3.4, 0.9, null, null, Later);

        Assert.Equal(areaId, area.Id);
        Assert.Equal(layout.Id, area.SeatingLayoutId);
        Assert.Contains(area, layout.Areas);
        Assert.Equal(Later, layout.UpdatedAt);
    }

    [Fact]
    public void FindArea_WhenPresent_ReturnsArea()
    {
        var layout = SeatingLayout.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Created);
        var area = layout.AddArea(Guid.NewGuid(), "Stage", AreaKind.Stage, AreaShape.Rect, 3.4, 0.9, null, null, Later);

        Assert.Same(area, layout.FindArea(area.Id));
    }

    [Fact]
    public void FindArea_WhenAbsent_ReturnsNull()
    {
        var layout = SeatingLayout.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Created);

        Assert.Null(layout.FindArea(Guid.NewGuid()));
    }

    [Fact]
    public void RemoveArea_WhenPresent_RemovesItAndBumpsUpdatedAt()
    {
        var layout = SeatingLayout.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Created);
        var area = layout.AddArea(Guid.NewGuid(), "Stage", AreaKind.Stage, AreaShape.Rect, 3.4, 0.9, null, null, Created);

        var removed = layout.RemoveArea(area.Id, Later);

        Assert.True(removed);
        Assert.Empty(layout.Areas);
        Assert.Equal(Later, layout.UpdatedAt);
    }

    [Fact]
    public void RemoveArea_WhenAbsent_ReturnsFalseAndDoesNotBumpUpdatedAt()
    {
        var layout = SeatingLayout.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Created);

        var removed = layout.RemoveArea(Guid.NewGuid(), Later);

        Assert.False(removed);
        Assert.Equal(Created, layout.UpdatedAt);
    }
}
