using FluentValidation;

namespace GuestManagementService.Application.Guests.ListGuests;

public sealed class ListGuestsQueryValidator : AbstractValidator<ListGuestsQuery>
{
    public ListGuestsQueryValidator()
    {
        RuleFor(query => query.EventId)
            .NotEmpty();
    }
}
