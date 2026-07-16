using FluentValidation;
using GuestManagementService.Application.Guests;
using GuestManagementService.Application.Guests.Wedding;

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

        RuleFor(command => command.Relationship)
            .Must(value => WeddingGuestMetadataMapper.TryParseRelationship(value, out _))
            .WithMessage("Relationship must be one of: Family, Friend, Colleague.");

        RuleFor(command => command.Side)
            .Must(value => WeddingGuestMetadataMapper.TryParseSide(value, out _))
            .WithMessage("Side must be one of: Bride, Groom.");

        RuleFor(command => command.PlusOnes)
            .GreaterThanOrEqualTo(0)
            .LessThanOrEqualTo(20)
            .When(command => command.PlusOnes.HasValue);

        RuleFor(command => command.DietaryNotes)
            .MaximumLength(500)
            .When(command => !string.IsNullOrWhiteSpace(command.DietaryNotes));
    }
}
