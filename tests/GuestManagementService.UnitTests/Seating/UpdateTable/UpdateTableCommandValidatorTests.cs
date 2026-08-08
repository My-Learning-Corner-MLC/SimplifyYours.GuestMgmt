using GuestManagementService.Application.Seating.UpdateTable;

namespace GuestManagementService.UnitTests.Seating.UpdateTable;

public sealed class UpdateTableCommandValidatorTests
{
    private readonly UpdateTableCommandValidator validator = new();

    [Fact]
    public void Validate_WhenCommandIsValid_HasNoErrors()
    {
        var result = validator.Validate(new UpdateTableCommand(Guid.NewGuid(), Guid.NewGuid(), "Family", "Round", 8, false));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WhenTableIdEmpty_HasError()
    {
        var result = validator.Validate(new UpdateTableCommand(Guid.NewGuid(), Guid.Empty, "Family", "Round", 8, false));

        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WhenNameBlank_HasError(string? name)
    {
        var result = validator.Validate(new UpdateTableCommand(Guid.NewGuid(), Guid.NewGuid(), name, "Round", 8, false));

        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(21)]
    public void Validate_WhenSeatCountOutOfRange_HasError(int seatCount)
    {
        var result = validator.Validate(new UpdateTableCommand(Guid.NewGuid(), Guid.NewGuid(), "Family", "Round", seatCount, false));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_WhenShapeUnrecognized_HasError()
    {
        var result = validator.Validate(new UpdateTableCommand(Guid.NewGuid(), Guid.NewGuid(), "Family", "Oval", 8, false));

        Assert.False(result.IsValid);
    }
}
