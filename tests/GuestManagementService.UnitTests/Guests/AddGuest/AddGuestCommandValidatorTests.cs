using GuestManagementService.Application.Guests.AddGuest;

namespace GuestManagementService.UnitTests.Guests.AddGuest;

public sealed class AddGuestCommandValidatorTests
{
    private readonly AddGuestCommandValidator validator = new();

    private static AddGuestCommand ValidCommand(
        string? email = "ada@example.com",
        string? phone = "+15551234567")
        => new(
            Guid.NewGuid(),
            "Ada",
            "Lovelace",
            phone,
            email,
            "preferNotToSay");

    [Fact]
    public void Validate_WhenAllFieldsAreValid_Passes()
    {
        var result = validator.Validate(ValidCommand());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WhenEventMetadataIsAbsent_Passes()
    {
        var result = validator.Validate(ValidCommand());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WhenEmailIsMissing_Fails()
    {
        var result = validator.Validate(ValidCommand(email: null));

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(AddGuestCommand.EmailAddress));
    }

    [Fact]
    public void Validate_WhenEmailIsInvalid_Fails()
    {
        var result = validator.Validate(ValidCommand(email: "not-email"));

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(AddGuestCommand.EmailAddress));
    }

    [Fact]
    public void Validate_WhenPhoneIsMissing_Fails()
    {
        var result = validator.Validate(ValidCommand(phone: null));

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(AddGuestCommand.PhoneNumber));
    }

    [Fact]
    public void Validate_WhenGenderIsUnknown_Fails()
    {
        var result = validator.Validate(new AddGuestCommand(
            Guid.NewGuid(),
            "Ada",
            "Lovelace",
            "+15551234567",
            "ada@example.com",
            "unknown"));

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(AddGuestCommand.Gender));
    }
}
