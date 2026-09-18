using FluentValidation;
using GuestManagementService.Domain.Seating;

namespace GuestManagementService.Application.Seating.UpdateTable;

public sealed class UpdateTableCommandValidator : AbstractValidator<UpdateTableCommand>
{
    public UpdateTableCommandValidator()
    {
        RuleFor(command => command.EventId)
            .NotEmpty();

        RuleFor(command => command.TableId)
            .NotEmpty();

        RuleFor(command => command.Name)
            .NotEmpty()
            .MaximumLength(120);

        RuleFor(command => command.Shape)
            .Must(value => SeatingParsing.TryParseShape(value, out _))
            .WithMessage("Shape must be one of: Round, Long, Square.");

        RuleFor(command => command.SeatCount)
            .InclusiveBetween(SeatingTable.SeatCountMin, SeatingTable.SeatCountMax);
    }
}
