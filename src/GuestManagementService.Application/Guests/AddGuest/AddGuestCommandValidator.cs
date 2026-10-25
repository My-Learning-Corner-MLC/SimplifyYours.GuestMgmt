using FluentValidation;
using GuestManagementService.Application.Guests;
using GuestManagementService.Domain.Guests;

namespace GuestManagementService.Application.Guests.AddGuest;

public sealed class AddGuestCommandValidator : AbstractValidator<AddGuestCommand>
{
    private static readonly string[] SupportedGenderValues =
    [
        "male",
        "female",
        "other",
        "preferNotToSay"
    ];

    public AddGuestCommandValidator()
    {
        RuleFor(command => command.EventId)
            .NotEmpty();

        RuleFor(command => command.FirstName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(command => command.LastName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(command => command.PhoneNumber)
            .NotEmpty()
            .MaximumLength(40)
            .Must(phone => !string.IsNullOrWhiteSpace(phone) && GuestNormalization.NormalizePhone(phone).Length > 0)
            .WithMessage("Phone number must contain at least one digit or leading plus sign.");

        RuleFor(command => command.EmailAddress)
            .NotEmpty()
            .MaximumLength(254)
            .EmailAddress();

        RuleFor(command => command.Gender)
            .Must(value => string.IsNullOrWhiteSpace(value)
                || SupportedGenderValues.Contains(value.Trim(), StringComparer.OrdinalIgnoreCase))
            .WithMessage("Gender must be one of: male, female, other, preferNotToSay.");

        RuleFor(command => command.Tags)
            .Must(tags => tags is null || tags.Count <= Guest.MaxTags)
            .WithMessage($"A guest may have at most {Guest.MaxTags} tags.");

        RuleForEach(command => command.Tags)
            .Must(tag => !string.IsNullOrWhiteSpace(tag))
            .WithMessage("Tags must not be blank.")
            .Must(tag => string.IsNullOrWhiteSpace(tag) || tag.Trim().Length <= Guest.MaxTagLength)
            .WithMessage($"Tags must be {Guest.MaxTagLength} characters or fewer.");
    }
}
