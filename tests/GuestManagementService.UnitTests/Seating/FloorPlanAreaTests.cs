using GuestManagementService.Domain.Seating;

namespace GuestManagementService.UnitTests.Seating;

public sealed class FloorPlanAreaTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 12, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_SetsFieldsAndDefaults()
    {
        var id = Guid.NewGuid();
        var layoutId = Guid.NewGuid();

        var area = FloorPlanArea.Create(
            id, layoutId, "  Photo booth  ", AreaKind.Custom, AreaShape.Rect, 2.4, 1.6, "#EFE3E8", 4, Now);

        Assert.Equal(id, area.Id);
        Assert.Equal(layoutId, area.SeatingLayoutId);
        Assert.Equal("Photo booth", area.Name);
        Assert.Equal(AreaKind.Custom, area.Kind);
        Assert.Equal(AreaShape.Rect, area.Shape);
        Assert.Equal(2.4, area.Width);
        Assert.Equal(1.6, area.Height);
        Assert.Equal("#EFE3E8", area.Color);
        Assert.Equal(4, area.Capacity);
        Assert.Null(area.PositionX);
        Assert.Null(area.PositionY);
        Assert.Equal(0, area.Rotation);
        Assert.Equal(Now, area.CreatedAt);
        Assert.Equal(Now, area.UpdatedAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WhenNameBlank_Throws(string name)
    {
        Assert.Throws<ArgumentException>(
            () => FloorPlanArea.Create(Guid.NewGuid(), Guid.NewGuid(), name, AreaKind.Custom, AreaShape.Rect, 2, 2, null, null, Now));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(200000)]
    public void Create_WhenWidthOutOfRange_Throws(double width)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => FloorPlanArea.Create(Guid.NewGuid(), Guid.NewGuid(), "Stage", AreaKind.Stage, AreaShape.Rect, width, 2, null, null, Now));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WhenCapacityNegative_Throws(int capacity)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => FloorPlanArea.Create(Guid.NewGuid(), Guid.NewGuid(), "Kids' corner", AreaKind.Custom, AreaShape.Rect, 2, 2, null, capacity, Now));
    }

    [Fact]
    public void Update_ChangesMetadataAndUpdatedAt()
    {
        var area = FloorPlanArea.Create(Guid.NewGuid(), Guid.NewGuid(), "Photo booth", AreaKind.Custom, AreaShape.Rect, 2.4, 1.6, "#EFE3E8", 4, Now);
        var later = Now.AddHours(1);

        area.Update("Gift table", AreaKind.Custom, AreaShape.Round, 1.2, 1.2, "#F6ECE0", null, later);

        Assert.Equal("Gift table", area.Name);
        Assert.Equal(AreaShape.Round, area.Shape);
        Assert.Equal(1.2, area.Width);
        Assert.Equal(1.2, area.Height);
        Assert.Equal("#F6ECE0", area.Color);
        Assert.Null(area.Capacity);
        Assert.Equal(later, area.UpdatedAt);
    }

    [Fact]
    public void Move_SetsPositionAndRotation()
    {
        var area = FloorPlanArea.Create(Guid.NewGuid(), Guid.NewGuid(), "Stage", AreaKind.Stage, AreaShape.Rect, 3.4, 0.9, null, null, Now);
        var later = Now.AddHours(1);

        area.Move(120, 40, 5, later);

        Assert.Equal(120, area.PositionX);
        Assert.Equal(40, area.PositionY);
        Assert.Equal(5, area.Rotation);
        Assert.Equal(later, area.UpdatedAt);
    }
}
