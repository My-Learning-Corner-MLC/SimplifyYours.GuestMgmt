using GuestManagementService.Application.Seating.DeleteTable;

namespace GuestManagementService.UnitTests.Seating.DeleteTable;

public sealed class DeleteTableCommandValidatorTests
{
    private readonly DeleteTableCommandValidator validator = new();

    [Fact]
    public void Validate_WhenCommandIsValid_HasNoErrors()
    {
        var result = validator.Validate(new DeleteTableCommand(Guid.NewGuid(), Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WhenEventIdEmpty_HasError()
    {
        var result = validator.Validate(new DeleteTableCommand(Guid.Empty, Guid.NewGuid()));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_WhenTableIdEmpty_HasError()
    {
        var result = validator.Validate(new DeleteTableCommand(Guid.NewGuid(), Guid.Empty));

        Assert.False(result.IsValid);
    }
}
