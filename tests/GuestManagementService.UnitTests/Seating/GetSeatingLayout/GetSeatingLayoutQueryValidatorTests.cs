using GuestManagementService.Application.Seating.GetSeatingLayout;

namespace GuestManagementService.UnitTests.Seating.GetSeatingLayout;

public sealed class GetSeatingLayoutQueryValidatorTests
{
    private readonly GetSeatingLayoutQueryValidator validator = new();

    [Fact]
    public void Validate_WhenEventIdIsSet_HasNoErrors()
    {
        var result = validator.Validate(new GetSeatingLayoutQuery(Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WhenEventIdEmpty_HasError()
    {
        var result = validator.Validate(new GetSeatingLayoutQuery(Guid.Empty));

        Assert.False(result.IsValid);
    }
}
