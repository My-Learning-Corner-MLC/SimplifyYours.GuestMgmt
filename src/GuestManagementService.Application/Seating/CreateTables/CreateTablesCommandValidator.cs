using FluentValidation;
using GuestManagementService.Domain.Seating;

namespace GuestManagementService.Application.Seating.CreateTables;

public sealed class CreateTablesCommandValidator : AbstractValidator<CreateTablesCommand>
{
    public CreateTablesCommandValidator()
    {
        RuleFor(command => command.EventId)
            .NotEmpty();

        RuleFor(command => command.Name)
            .NotEmpty()
            .MaximumLength(120);

        RuleFor(command => command.Shape)
            .Must(value => SeatingParsing.TryParseShape(value, out _))
            .WithMessage("Shape must be one of: Round, Long, Square.");

        RuleFor(command => command.SeatCount)
            .InclusiveBetween(SeatingTable.SeatCountMin, SeatingTable.SeatCountMax);

        RuleFor(command => command.Count)
            .InclusiveBetween(1, 20);
    }
}
