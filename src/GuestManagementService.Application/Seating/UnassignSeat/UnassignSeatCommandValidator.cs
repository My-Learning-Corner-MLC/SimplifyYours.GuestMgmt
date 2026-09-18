using FluentValidation;

namespace GuestManagementService.Application.Seating.UnassignSeat;

public sealed class UnassignSeatCommandValidator : AbstractValidator<UnassignSeatCommand>
{
    public UnassignSeatCommandValidator()
    {
        RuleFor(command => command.EventId)
            .NotEmpty();

        RuleFor(command => command.TableId)
            .NotEmpty();

        RuleFor(command => command.SeatIndex)
            .GreaterThanOrEqualTo(0);
    }
}
