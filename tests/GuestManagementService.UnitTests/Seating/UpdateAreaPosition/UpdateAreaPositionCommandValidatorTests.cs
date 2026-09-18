using GuestManagementService.Application.Seating.UpdateAreaPosition;

namespace GuestManagementService.UnitTests.Seating.UpdateAreaPosition;

public sealed class UpdateAreaPositionCommandValidatorTests
{
    private readonly UpdateAreaPositionCommandValidator validator = new();

    [Fact]
    public void Validate_WhenCommandIsValid_HasNoErrors()
    {
        var result = validator.Validate(new UpdateAreaPositionCommand(Guid.NewGuid(), Guid.NewGuid(), 100, 20, 5));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WhenAreaIdEmpty_HasError()
    {
        var result = validator.Validate(new UpdateAreaPositionCommand(Guid.NewGuid(), Guid.Empty, 100, 20, 5));

        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(200000)]
    public void Validate_WhenPositionXNotFiniteOrOutOfRange_HasError(double positionX)
    {
        var result = validator.Validate(new UpdateAreaPositionCommand(Guid.NewGuid(), Guid.NewGuid(), positionX, 20, 5));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_WhenRotationNotFinite_HasError()
    {
        var result = validator.Validate(new UpdateAreaPositionCommand(Guid.NewGuid(), Guid.NewGuid(), 100, 20, double.PositiveInfinity));

        Assert.False(result.IsValid);
    }
}
