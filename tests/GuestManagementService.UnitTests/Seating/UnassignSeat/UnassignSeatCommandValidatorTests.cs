using GuestManagementService.Application.Seating.UnassignSeat;

namespace GuestManagementService.UnitTests.Seating.UnassignSeat;

public sealed class UnassignSeatCommandValidatorTests
{
    private readonly UnassignSeatCommandValidator validator = new();

    [Fact]
    public void Validate_WhenCommandIsValid_HasNoErrors()
    {
        var result = validator.Validate(new UnassignSeatCommand(Guid.NewGuid(), Guid.NewGuid(), 0));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WhenEventIdEmpty_HasError()
    {
        var result = validator.Validate(new UnassignSeatCommand(Guid.Empty, Guid.NewGuid(), 0));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_WhenTableIdEmpty_HasError()
    {
        var result = validator.Validate(new UnassignSeatCommand(Guid.NewGuid(), Guid.Empty, 0));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_WhenSeatIndexNegative_HasError()
    {
        var result = validator.Validate(new UnassignSeatCommand(Guid.NewGuid(), Guid.NewGuid(), -1));

        Assert.False(result.IsValid);
    }
}
