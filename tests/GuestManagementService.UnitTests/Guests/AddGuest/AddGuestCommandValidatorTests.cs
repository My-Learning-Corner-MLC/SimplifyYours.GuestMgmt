using GuestManagementService.Application.Guests.AddGuest;

namespace GuestManagementService.UnitTests.Guests.AddGuest;

public sealed class AddGuestCommandValidatorTests
{
    private readonly AddGuestCommandValidator validator = new();

    [Fact]
    public void Validate_WhenRequiredFieldsAreValid_Passes()
    {
        var result = validator.Validate(new AddGuestCommand(
            Guid.NewGuid(),
            "Ada",
            "Lovelace",
            "+15551234567",
            null,
            null));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WhenEmailIsInvalid_Fails()
    {
        var result = validator.Validate(new AddGuestCommand(
            Guid.NewGuid(),
            "Ada",
            "Lovelace",
            "+15551234567",
            "not-email",
            "preferNotToSay"));

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(AddGuestCommand.EmailAddress));
    }

    [Fact]
    public void Validate_WhenGenderIsUnknown_Fails()
    {
        var result = validator.Validate(new AddGuestCommand(
            Guid.NewGuid(),
            "Ada",
            "Lovelace",
            "+15551234567",
            null,
            "unknown"));

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(AddGuestCommand.Gender));
    }
}
