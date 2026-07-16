using FluentValidation;

namespace GuestManagementService.Application.Guests.ListGuests;

public sealed class ListGuestsQueryValidator : AbstractValidator<ListGuestsQuery>
{
    private static readonly HashSet<string> SortFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "name",
        "email",
        "createdAt"
    };

    private static readonly HashSet<string> SortDirections = new(StringComparer.OrdinalIgnoreCase)
    {
        "asc",
        "desc"
    };

    public ListGuestsQueryValidator()
    {
        RuleFor(query => query.EventId)
            .NotEmpty();

        RuleFor(query => query.PageNumber)
            .GreaterThanOrEqualTo(1)
            .When(query => query.PageNumber.HasValue);

        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, ListGuestsQueryDefaults.MaxPageSize)
            .When(query => query.PageSize.HasValue);

        RuleFor(query => query.SortBy)
            .Must(value => BeEmptyOrOneOf(value, SortFields))
            .WithMessage("Sort field must be one of: name, email, createdAt.");

        RuleFor(query => query.SortDirection)
            .Must(value => BeEmptyOrOneOf(value, SortDirections))
            .WithMessage("Sort direction must be one of: asc, desc.");
    }

    private static bool BeEmptyOrOneOf(string? value, HashSet<string> acceptedValues)
    {
        return string.IsNullOrWhiteSpace(value) || acceptedValues.Contains(value.Trim());
    }
}
