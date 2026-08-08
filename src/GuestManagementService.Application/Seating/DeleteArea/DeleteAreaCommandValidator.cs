using FluentValidation;

namespace GuestManagementService.Application.Seating.DeleteArea;

public sealed class DeleteAreaCommandValidator : AbstractValidator<DeleteAreaCommand>
{
    public DeleteAreaCommandValidator()
    {
        RuleFor(command => command.EventId)
            .NotEmpty();

        RuleFor(command => command.AreaId)
            .NotEmpty();
    }
}
