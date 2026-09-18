using GuestManagementService.Application.Seating.ApplyTablePositionsBatch;

namespace GuestManagementService.UnitTests.Seating.ApplyTablePositionsBatch;

public sealed class ApplyTablePositionsBatchCommandValidatorTests
{
    private readonly ApplyTablePositionsBatchCommandValidator validator = new();

    [Fact]
    public void Validate_WhenCommandIsValid_HasNoErrors()
    {
        var positions = new[] { new TablePositionInput(Guid.NewGuid(), 10, 20, 0) };

        var result = validator.Validate(new ApplyTablePositionsBatchCommand(Guid.NewGuid(), positions));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WhenPositionsEmpty_HasError()
    {
        var result = validator.Validate(new ApplyTablePositionsBatchCommand(Guid.NewGuid(), []));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_WhenPositionsExceedMax_HasError()
    {
        var positions = Enumerable.Range(0, ApplyTablePositionsBatchCommandValidator.MaxPositions + 1)
            .Select(_ => new TablePositionInput(Guid.NewGuid(), 0, 0, 0))
            .ToList();

        var result = validator.Validate(new ApplyTablePositionsBatchCommand(Guid.NewGuid(), positions));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_WhenTableIdEmpty_HasError()
    {
        var positions = new[] { new TablePositionInput(Guid.Empty, 0, 0, 0) };

        var result = validator.Validate(new ApplyTablePositionsBatchCommand(Guid.NewGuid(), positions));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_WhenPositionNotFinite_HasError()
    {
        var positions = new[] { new TablePositionInput(Guid.NewGuid(), double.NaN, 0, 0) };

        var result = validator.Validate(new ApplyTablePositionsBatchCommand(Guid.NewGuid(), positions));

        Assert.False(result.IsValid);
    }
}
