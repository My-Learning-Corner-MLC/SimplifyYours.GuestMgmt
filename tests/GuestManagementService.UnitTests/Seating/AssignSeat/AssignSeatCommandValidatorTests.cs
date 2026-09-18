using GuestManagementService.Application.Seating.AssignSeat;

namespace GuestManagementService.UnitTests.Seating.AssignSeat;

public sealed class AssignSeatCommandValidatorTests
{
    private readonly AssignSeatCommandValidator validator = new();

    [Fact]
    public void Validate_WhenCommandIsValid_HasNoErrors()
    {
        var result = validator.Validate(new AssignSeatCommand(Guid.NewGuid(), Guid.NewGuid(), 0, Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WhenEventIdEmpty_HasError()
    {
        var result = validator.Validate(new AssignSeatCommand(Guid.Empty, Guid.NewGuid(), 0, Guid.NewGuid()));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_WhenTableIdEmpty_HasError()
    {
        var result = validator.Validate(new AssignSeatCommand(Guid.NewGuid(), Guid.Empty, 0, Guid.NewGuid()));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_WhenGuestIdEmpty_HasError()
    {
        var result = validator.Validate(new AssignSeatCommand(Guid.NewGuid(), Guid.NewGuid(), 0, Guid.Empty));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_WhenSeatIndexNegative_HasError()
    {
        var result = validator.Validate(new AssignSeatCommand(Guid.NewGuid(), Guid.NewGuid(), -1, Guid.NewGuid()));

        Assert.False(result.IsValid);
    }
}
