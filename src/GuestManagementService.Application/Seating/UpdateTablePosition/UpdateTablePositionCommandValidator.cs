using FluentValidation;

namespace GuestManagementService.Application.Seating.UpdateTablePosition;

public sealed class UpdateTablePositionCommandValidator : AbstractValidator<UpdateTablePositionCommand>
{
    public UpdateTablePositionCommandValidator()
    {
        RuleFor(command => command.EventId)
            .NotEmpty();

        RuleFor(command => command.TableId)
            .NotEmpty();

        RuleFor(command => command.PositionX)
            .Must(double.IsFinite)
            .InclusiveBetween(SeatingPositionBounds.MinCoordinate, SeatingPositionBounds.MaxCoordinate);

        RuleFor(command => command.PositionY)
            .Must(double.IsFinite)
            .InclusiveBetween(SeatingPositionBounds.MinCoordinate, SeatingPositionBounds.MaxCoordinate);

        RuleFor(command => command.Rotation)
            .Must(double.IsFinite);
    }
}
