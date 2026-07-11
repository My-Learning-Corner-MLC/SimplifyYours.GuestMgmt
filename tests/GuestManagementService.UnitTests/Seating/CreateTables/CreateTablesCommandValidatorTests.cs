using GuestManagementService.Application.Seating.CreateTables;

namespace GuestManagementService.UnitTests.Seating.CreateTables;

public sealed class CreateTablesCommandValidatorTests
{
    private readonly CreateTablesCommandValidator validator = new();

    [Fact]
    public void Validate_WhenCommandIsValid_HasNoErrors()
    {
        var result = validator.Validate(new CreateTablesCommand(Guid.NewGuid(), "Family", "Round", 8, 1));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WhenEventIdEmpty_HasError()
    {
        var result = validator.Validate(new CreateTablesCommand(Guid.Empty, "Family", "Round", 8, 1));

        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_WhenNameBlank_HasError(string? name)
    {
        var result = validator.Validate(new CreateTablesCommand(Guid.NewGuid(), name, "Round", 8, 1));

        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData("Round")]
    [InlineData("round")]
    [InlineData("Long")]
    [InlineData("Square")]
    public void Validate_WhenShapeRecognized_HasNoError(string shape)
    {
        var result = validator.Validate(new CreateTablesCommand(Guid.NewGuid(), "Family", shape, 8, 1));

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("Oval")]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WhenShapeUnrecognized_HasError(string? shape)
    {
        var result = validator.Validate(new CreateTablesCommand(Guid.NewGuid(), "Family", shape, 8, 1));

        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(21)]
    public void Validate_WhenSeatCountOutOfRange_HasError(int seatCount)
    {
        var result = validator.Validate(new CreateTablesCommand(Guid.NewGuid(), "Family", "Round", seatCount, 1));

        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(21)]
    public void Validate_WhenCountOutOfRange_HasError(int count)
    {
        var result = validator.Validate(new CreateTablesCommand(Guid.NewGuid(), "Family", "Round", 8, count));

        Assert.False(result.IsValid);
    }
}
