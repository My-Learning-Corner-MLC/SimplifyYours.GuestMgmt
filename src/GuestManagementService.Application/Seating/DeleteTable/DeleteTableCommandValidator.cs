using FluentValidation;

namespace GuestManagementService.Application.Seating.DeleteTable;

public sealed class DeleteTableCommandValidator : AbstractValidator<DeleteTableCommand>
{
    public DeleteTableCommandValidator()
    {
        RuleFor(command => command.EventId)
            .NotEmpty();

        RuleFor(command => command.TableId)
            .NotEmpty();
    }
}
