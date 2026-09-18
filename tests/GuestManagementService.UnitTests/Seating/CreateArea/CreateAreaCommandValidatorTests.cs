using GuestManagementService.Application.Seating.CreateArea;

namespace GuestManagementService.UnitTests.Seating.CreateArea;

public sealed class CreateAreaCommandValidatorTests
{
    private readonly CreateAreaCommandValidator validator = new();

    [Fact]
    public void Validate_WhenCommandIsValid_HasNoErrors()
    {
        var result = validator.Validate(new CreateAreaCommand(Guid.NewGuid(), "Photo booth", "Custom", "Rect", 2.4, 1.6, "#EFE3E8", 4));

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WhenNameBlank_HasError(string? name)
    {
        var result = validator.Validate(new CreateAreaCommand(Guid.NewGuid(), name, "Custom", "Rect", 2, 2, null, null));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_WhenKindUnrecognized_HasError()
    {
        var result = validator.Validate(new CreateAreaCommand(Guid.NewGuid(), "Photo booth", "Unknown", "Rect", 2, 2, null, null));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_WhenShapeUnrecognized_HasError()
    {
        var result = validator.Validate(new CreateAreaCommand(Guid.NewGuid(), "Photo booth", "Custom", "Hex", 2, 2, null, null));

        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WhenWidthOutOfRange_HasError(double width)
    {
        var result = validator.Validate(new CreateAreaCommand(Guid.NewGuid(), "Photo booth", "Custom", "Rect", width, 2, null, null));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_WhenCapacityZero_HasError()
    {
        var result = validator.Validate(new CreateAreaCommand(Guid.NewGuid(), "Photo booth", "Custom", "Rect", 2, 2, null, 0));

        Assert.False(result.IsValid);
    }
}
