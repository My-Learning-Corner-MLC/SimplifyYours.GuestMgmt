using GuestManagementService.Domain.Seating;

namespace GuestManagementService.UnitTests.Seating;

public sealed class SeatingTableTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 12, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_SetsFieldsAndDefaults()
    {
        var layoutId = Guid.NewGuid();
        var tableId = Guid.NewGuid();

        var table = SeatingTable.Create(tableId, layoutId, "  Family  ", TableShape.Round, 8, Now);

        Assert.Equal(tableId, table.Id);
        Assert.Equal(layoutId, table.SeatingLayoutId);
        Assert.Equal("Family", table.Name);
        Assert.Equal(TableShape.Round, table.Shape);
        Assert.Equal(8, table.SeatCount);
        Assert.False(table.IsFull);
        Assert.Null(table.PositionX);
        Assert.Null(table.PositionY);
        Assert.Equal(0, table.Rotation);
        Assert.Equal(Now, table.CreatedAt);
        Assert.Equal(Now, table.UpdatedAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WhenNameBlank_Throws(string name)
    {
        var exception = Assert.Throws<ArgumentException>(
            () => SeatingTable.Create(Guid.NewGuid(), Guid.NewGuid(), name, TableShape.Round, 8, Now));

        Assert.Equal("name", exception.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    [InlineData(21)]
    public void Create_WhenSeatCountOutOfRange_Throws(int seatCount)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => SeatingTable.Create(Guid.NewGuid(), Guid.NewGuid(), "Family", TableShape.Round, seatCount, Now));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(20)]
    public void Create_AtSeatCountBounds_Succeeds(int seatCount)
    {
        var table = SeatingTable.Create(Guid.NewGuid(), Guid.NewGuid(), "Family", TableShape.Long, seatCount, Now);

        Assert.Equal(seatCount, table.SeatCount);
    }

    [Fact]
    public void Create_WhenLayoutIdEmpty_Throws()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => SeatingTable.Create(Guid.NewGuid(), Guid.Empty, "Family", TableShape.Round, 8, Now));

        Assert.Equal("seatingLayoutId", exception.ParamName);
    }
}
