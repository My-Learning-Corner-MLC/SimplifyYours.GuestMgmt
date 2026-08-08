using GuestManagementService.Application.Seating.UpdateTablePosition;

namespace GuestManagementService.UnitTests.Seating.UpdateTablePosition;

public sealed class UpdateTablePositionCommandValidatorTests
{
    private readonly UpdateTablePositionCommandValidator validator = new();

    [Fact]
    public void Validate_WhenCommandIsValid_HasNoErrors()
    {
        var result = validator.Validate(new UpdateTablePositionCommand(Guid.NewGuid(), Guid.NewGuid(), 340, 420, 15));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WhenTableIdEmpty_HasError()
    {
        var result = validator.Validate(new UpdateTablePositionCommand(Guid.NewGuid(), Guid.Empty, 340, 420, 15));

        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(200000)]
    public void Validate_WhenPositionXNotFiniteOrOutOfRange_HasError(double positionX)
    {
        var result = validator.Validate(new UpdateTablePositionCommand(Guid.NewGuid(), Guid.NewGuid(), positionX, 420, 15));

        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(-200000)]
    public void Validate_WhenPositionYNotFiniteOrOutOfRange_HasError(double positionY)
    {
        var result = validator.Validate(new UpdateTablePositionCommand(Guid.NewGuid(), Guid.NewGuid(), 340, positionY, 15));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_WhenRotationNotFinite_HasError()
    {
        var result = validator.Validate(new UpdateTablePositionCommand(Guid.NewGuid(), Guid.NewGuid(), 340, 420, double.NaN));

        Assert.False(result.IsValid);
    }
}
