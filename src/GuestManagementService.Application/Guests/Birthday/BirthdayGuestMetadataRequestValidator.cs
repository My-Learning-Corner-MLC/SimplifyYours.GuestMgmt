using FluentValidation;

namespace GuestManagementService.Application.Guests.Birthday;

public sealed class BirthdayGuestMetadataRequestValidator : AbstractValidator<BirthdayGuestMetadataRequest>
{
    public BirthdayGuestMetadataRequestValidator()
    {
        RuleFor(request => request.PlusOnes)
            .InclusiveBetween(0, 20)
            .When(request => request.PlusOnes.HasValue)
            .WithMessage("Plus-ones must be between 0 and 20.");

        RuleFor(request => request.DietaryNotes)
            .MaximumLength(500)
            .WithMessage("Dietary notes must be 500 characters or fewer.");
    }
}
