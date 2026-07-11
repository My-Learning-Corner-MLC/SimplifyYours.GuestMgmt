using FluentValidation;
using GuestManagementService.Domain.Seating;

namespace GuestManagementService.Application.Seating.UpdateArea;

public sealed class UpdateAreaCommandValidator : AbstractValidator<UpdateAreaCommand>
{
    public UpdateAreaCommandValidator()
    {
        RuleFor(command => command.EventId)
            .NotEmpty();

        RuleFor(command => command.AreaId)
            .NotEmpty();

        RuleFor(command => command.Name)
            .NotEmpty()
            .MaximumLength(120);

        RuleFor(command => command.Kind)
            .Must(value => SeatingParsing.TryParseAreaKind(value, out _))
            .WithMessage("Kind must be one of: Stage, DanceFloor, Bar, Entrance, Buffet, Cake, Custom.");

        RuleFor(command => command.Shape)
            .Must(value => SeatingParsing.TryParseAreaShape(value, out _))
            .WithMessage("Shape must be one of: Rect, Round, Free.");

        RuleFor(command => command.Width)
            .InclusiveBetween(FloorPlanArea.MinDimension, FloorPlanArea.MaxDimension);

        RuleFor(command => command.Height)
            .InclusiveBetween(FloorPlanArea.MinDimension, FloorPlanArea.MaxDimension);

        RuleFor(command => command.Color)
            .MaximumLength(32);

        RuleFor(command => command.Capacity)
            .InclusiveBetween(FloorPlanArea.CapacityMin, FloorPlanArea.CapacityMax)
            .When(command => command.Capacity.HasValue);
    }
}
