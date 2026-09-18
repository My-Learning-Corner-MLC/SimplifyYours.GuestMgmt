using GuestManagementService.Domain.Seating;

namespace GuestManagementService.UnitTests.Seating;

public sealed class SeatAdjacencyTests
{
    [Fact]
    public void FindContiguousFreeSeats_WhenRequiredCountIsZero_ReturnsEmptyList()
    {
        var result = SeatAdjacency.FindContiguousFreeSeats(TableShape.Round, 8, 2, 0, new HashSet<int>());

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void FindContiguousFreeSeats_WhenAnchorSeatOccupied_ReturnsNull()
    {
        var occupied = new HashSet<int> { 2 };

        var result = SeatAdjacency.FindContiguousFreeSeats(TableShape.Round, 8, 2, 1, occupied);

        Assert.Null(result);
    }

    [Fact]
    public void FindContiguousFreeSeats_WhenRequiredCountExceedsSeatCount_ReturnsNull()
    {
        var result = SeatAdjacency.FindContiguousFreeSeats(TableShape.Round, 4, 0, 5, new HashSet<int>());

        Assert.Null(result);
    }

    [Fact]
    public void FindContiguousFreeSeats_Round_AlwaysIncludesAnchorFirst()
    {
        var result = SeatAdjacency.FindContiguousFreeSeats(TableShape.Round, 8, 5, 3, new HashSet<int>());

        Assert.NotNull(result);
        Assert.Equal(5, result[0]);
    }

    [Fact]
    public void FindContiguousFreeSeats_Round_BalancesBothSidesOfTheAnchor()
    {
        var result = SeatAdjacency.FindContiguousFreeSeats(TableShape.Round, 8, 2, 3, new HashSet<int>());

        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Equal(2, result[0]);
        Assert.Equal(new[] { 1, 3 }, result.Skip(1).OrderBy(index => index));
    }

    [Fact]
    public void FindContiguousFreeSeats_Round_WrapsAroundTheRing()
    {
        // 4-seat ring, anchor at 0: neighbors are 1 (right) and 3 (left, via wraparound).
        var result = SeatAdjacency.FindContiguousFreeSeats(TableShape.Round, 4, 0, 3, new HashSet<int>());

        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Contains(0, result);
        Assert.Contains(1, result);
        Assert.Contains(3, result);
    }

    [Fact]
    public void FindContiguousFreeSeats_Round_StopsAtOccupiedSeatsOnEachSide()
    {
        // 8-seat ring, seats 1 and 6 occupied; anchor 3 can only reach 2,3,4,5.
        var occupied = new HashSet<int> { 1, 6 };

        var result = SeatAdjacency.FindContiguousFreeSeats(TableShape.Round, 8, 3, 4, occupied);

        Assert.NotNull(result);
        Assert.Equal(4, result.Count);
        Assert.DoesNotContain(1, result);
        Assert.DoesNotContain(6, result);
    }

    [Fact]
    public void FindContiguousFreeSeats_Round_WhenBoxedInByOccupiedSeats_ReturnsNull()
    {
        var occupied = new HashSet<int> { 1, 3 };

        var result = SeatAdjacency.FindContiguousFreeSeats(TableShape.Round, 4, 0, 2, occupied);

        Assert.Null(result);
    }

    [Fact]
    public void FindContiguousFreeSeats_Round_WhenPartyFillsTheEntireTable_DoesNotDoubleCount()
    {
        // Full ring, no occupancy: a party of 8 on an 8-seat table must claim every seat
        // exactly once, even though the two search directions meet on the far side.
        var result = SeatAdjacency.FindContiguousFreeSeats(TableShape.Round, 8, 0, 8, new HashSet<int>());

        Assert.NotNull(result);
        Assert.Equal(8, result.Count);
        Assert.Equal(Enumerable.Range(0, 8), result.OrderBy(index => index));
    }

    [Fact]
    public void FindContiguousFreeSeats_Square_TreatsIndexOrderAsARing()
    {
        var result = SeatAdjacency.FindContiguousFreeSeats(TableShape.Square, 4, 0, 3, new HashSet<int>());

        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Contains(0, result);
        Assert.Contains(1, result);
        Assert.Contains(3, result);
    }

    [Fact]
    public void FindContiguousFreeSeats_Long_DoesNotWrapAcrossTheRowBreak()
    {
        // 6-seat long table: top row is [0,1,2], bottom row is [3,4,5] (see RowBounds).
        // Anchor at 2 (last top seat) can only reach into the top row, not seat 3.
        var result = SeatAdjacency.FindContiguousFreeSeats(TableShape.Long, 6, 2, 3, new HashSet<int>());

        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Equal(new[] { 0, 1, 2 }, result.OrderBy(index => index));
        Assert.DoesNotContain(3, result);
    }

    [Fact]
    public void FindContiguousFreeSeats_Long_WhenRowTooShortForParty_ReturnsNullEvenIfOtherRowIsFree()
    {
        // Top row only has 3 seats; a party of 4 anchored there can't spill into the bottom row.
        var result = SeatAdjacency.FindContiguousFreeSeats(TableShape.Long, 6, 1, 4, new HashSet<int>());

        Assert.Null(result);
    }

    [Fact]
    public void FindContiguousFreeSeats_Long_BottomRowSeatsAreAdjacentToEachOther()
    {
        // Bottom row is [3,4,5] on a 6-seat long table; anchor at 4 reaches 3 and 5.
        var result = SeatAdjacency.FindContiguousFreeSeats(TableShape.Long, 6, 4, 3, new HashSet<int>());

        Assert.NotNull(result);
        Assert.Equal(new[] { 3, 4, 5 }, result.OrderBy(index => index));
    }

    [Fact]
    public void FindContiguousFreeSeats_Long_OddSeatCountGivesTheExtraSeatToTheTopRow()
    {
        // 5-seat long table: top row [0,1,2] (3 seats), bottom row [3,4] (2 seats).
        var topRowFull = SeatAdjacency.FindContiguousFreeSeats(TableShape.Long, 5, 0, 3, new HashSet<int>());
        Assert.NotNull(topRowFull);
        Assert.Equal(new[] { 0, 1, 2 }, topRowFull.OrderBy(index => index));

        var bottomRowTooSmall = SeatAdjacency.FindContiguousFreeSeats(TableShape.Long, 5, 3, 3, new HashSet<int>());
        Assert.Null(bottomRowTooSmall);
    }
}
