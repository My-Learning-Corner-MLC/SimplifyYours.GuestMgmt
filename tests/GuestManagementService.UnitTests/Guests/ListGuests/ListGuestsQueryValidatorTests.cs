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

    [Fact]
    public void Validate_WhenPageNumberIsZero_Fails()
    {
        var result = validator.Validate(new ListGuestsQuery(Guid.NewGuid(), PageNumber: 0));

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(ListGuestsQuery.PageNumber));
    }

    [Fact]
    public void Validate_WhenPageSizeExceedsMax_Fails()
    {
        var result = validator.Validate(
            new ListGuestsQuery(Guid.NewGuid(), PageSize: ListGuestsQueryDefaults.MaxPageSize + 1));

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(ListGuestsQuery.PageSize));
    }

    [Fact]
    public void Validate_WhenSortByIsUnknown_Fails()
    {
        var result = validator.Validate(new ListGuestsQuery(Guid.NewGuid(), SortBy: "table"));

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(ListGuestsQuery.SortBy));
    }

    [Fact]
    public void Validate_WhenSortDirectionIsUnknown_Fails()
    {
        var result = validator.Validate(new ListGuestsQuery(Guid.NewGuid(), SortDirection: "sideways"));

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(ListGuestsQuery.SortDirection));
    }

    [Theory]
    [InlineData("name")]
    [InlineData("email")]
    [InlineData("createdAt")]
    public void Validate_WhenSortByIsKnown_Passes(string sortBy)
    {
        var result = validator.Validate(new ListGuestsQuery(Guid.NewGuid(), SortBy: sortBy));

        Assert.True(result.IsValid);
    }
}
