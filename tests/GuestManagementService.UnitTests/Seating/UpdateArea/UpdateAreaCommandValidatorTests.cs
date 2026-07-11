using GuestManagementService.Application.Seating.UpdateArea;

namespace GuestManagementService.UnitTests.Seating.UpdateArea;

public sealed class UpdateAreaCommandValidatorTests
{
    private readonly UpdateAreaCommandValidator validator = new();

    [Fact]
    public void Validate_WhenCommandIsValid_HasNoErrors()
    {
        var result = validator.Validate(new UpdateAreaCommand(Guid.NewGuid(), Guid.NewGuid(), "Gift table", "Custom", "Round", 1.2, 1.2, "#F6ECE0", 2));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WhenAreaIdEmpty_HasError()
    {
        var result = validator.Validate(new UpdateAreaCommand(Guid.NewGuid(), Guid.Empty, "Gift table", "Custom", "Round", 1.2, 1.2, null, null));

        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WhenNameBlank_HasError(string? name)
    {
        var result = validator.Validate(new UpdateAreaCommand(Guid.NewGuid(), Guid.NewGuid(), name, "Custom", "Round", 1.2, 1.2, null, null));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_WhenKindUnrecognized_HasError()
    {
        var result = validator.Validate(new UpdateAreaCommand(Guid.NewGuid(), Guid.NewGuid(), "Gift table", "Unknown", "Round", 1.2, 1.2, null, null));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_WhenShapeUnrecognized_HasError()
    {
        var result = validator.Validate(new UpdateAreaCommand(Guid.NewGuid(), Guid.NewGuid(), "Gift table", "Custom", "Hex", 1.2, 1.2, null, null));

        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WhenWidthOutOfRange_HasError(double width)
    {
        var result = validator.Validate(new UpdateAreaCommand(Guid.NewGuid(), Guid.NewGuid(), "Gift table", "Custom", "Round", width, 1.2, null, null));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_WhenCapacityZero_HasError()
    {
        var result = validator.Validate(new UpdateAreaCommand(Guid.NewGuid(), Guid.NewGuid(), "Gift table", "Custom", "Round", 1.2, 1.2, null, 0));

        Assert.False(result.IsValid);
    }
}
