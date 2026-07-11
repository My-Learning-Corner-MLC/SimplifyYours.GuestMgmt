using FluentValidation;

namespace GuestManagementService.Application.Seating.GetSeatingLayout;

public sealed class GetSeatingLayoutQueryValidator : AbstractValidator<GetSeatingLayoutQuery>
{
    public GetSeatingLayoutQueryValidator()
    {
        RuleFor(query => query.EventId)
            .NotEmpty();
    }
}
