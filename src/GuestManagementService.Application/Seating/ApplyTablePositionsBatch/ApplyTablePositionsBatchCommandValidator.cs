using FluentValidation;

namespace GuestManagementService.Application.Seating.ApplyTablePositionsBatch;

public sealed class ApplyTablePositionsBatchCommandValidator : AbstractValidator<ApplyTablePositionsBatchCommand>
{
    public const int MaxPositions = 200;

    public ApplyTablePositionsBatchCommandValidator()
    {
        RuleFor(command => command.EventId)
            .NotEmpty();

        RuleFor(command => command.Positions)
            .NotEmpty()
            .Must(positions => positions.Count <= MaxPositions)
            .WithMessage($"A batch may contain at most {MaxPositions} positions.");

        RuleForEach(command => command.Positions).ChildRules(position =>
        {
            position.RuleFor(p => p.TableId)
                .NotEmpty();

            position.RuleFor(p => p.PositionX)
                .Must(double.IsFinite)
                .InclusiveBetween(SeatingPositionBounds.MinCoordinate, SeatingPositionBounds.MaxCoordinate);

            position.RuleFor(p => p.PositionY)
                .Must(double.IsFinite)
                .InclusiveBetween(SeatingPositionBounds.MinCoordinate, SeatingPositionBounds.MaxCoordinate);

            position.RuleFor(p => p.Rotation)
                .Must(double.IsFinite);
        });
    }
}
