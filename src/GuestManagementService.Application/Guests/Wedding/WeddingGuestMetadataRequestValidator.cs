using FluentValidation;

namespace GuestManagementService.Application.Guests.Wedding;

public sealed class WeddingGuestMetadataRequestValidator : AbstractValidator<WeddingGuestMetadataRequest>
{
    public WeddingGuestMetadataRequestValidator()
    {
        RuleFor(request => request.Relationship)
            .Must(value => WeddingGuestMetadataMapper.TryParseRelationship(value, out _))
            .WithMessage("Relationship must be one of: Family, Friend, Colleague.");

        RuleFor(request => request.Side)
            .Must(value => WeddingGuestMetadataMapper.TryParseSide(value, out _))
            .WithMessage("Side must be one of: Bride, Groom.");

        RuleFor(request => request.PlusOnes)
            .InclusiveBetween(0, 20)
            .When(request => request.PlusOnes.HasValue)
            .WithMessage("Plus-ones must be between 0 and 20.");

        RuleFor(request => request.DietaryNotes)
            .MaximumLength(500)
            .WithMessage("Dietary notes must be 500 characters or fewer.");
    }
}
