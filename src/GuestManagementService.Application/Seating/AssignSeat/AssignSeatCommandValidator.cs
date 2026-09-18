using FluentValidation;

namespace GuestManagementService.Application.Seating.AssignSeat;

public sealed class AssignSeatCommandValidator : AbstractValidator<AssignSeatCommand>
{
    public AssignSeatCommandValidator()
    {
        RuleFor(command => command.EventId)
            .NotEmpty();

        RuleFor(command => command.TableId)
            .NotEmpty();

        RuleFor(command => command.GuestId)
            .NotEmpty();

        RuleFor(command => command.SeatIndex)
            .GreaterThanOrEqualTo(0);
    }
}
