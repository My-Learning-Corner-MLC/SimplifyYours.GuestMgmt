using FluentValidation;

namespace GuestManagementService.Application.Seating.UpdateAreaPosition;

public sealed class UpdateAreaPositionCommandValidator : AbstractValidator<UpdateAreaPositionCommand>
{
    public UpdateAreaPositionCommandValidator()
    {
        RuleFor(command => command.EventId)
            .NotEmpty();

        RuleFor(command => command.AreaId)
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
