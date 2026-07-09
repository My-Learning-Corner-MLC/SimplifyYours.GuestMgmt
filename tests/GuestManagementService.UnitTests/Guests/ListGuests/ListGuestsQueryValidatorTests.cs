using GuestManagementService.Application.Guests.ListGuests;

namespace GuestManagementService.UnitTests.Guests.ListGuests;

public sealed class ListGuestsQueryValidatorTests
{
    private readonly ListGuestsQueryValidator validator = new();

    [Fact]
    public void Validate_WhenEventIdProvided_Passes()
    {
        var result = validator.Validate(new ListGuestsQuery(Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WhenEventIdEmpty_Fails()
    {
        var result = validator.Validate(new ListGuestsQuery(Guid.Empty));

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(ListGuestsQuery.EventId));
    }
}
