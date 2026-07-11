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
}
