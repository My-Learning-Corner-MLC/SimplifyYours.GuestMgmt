using FluentValidation;

namespace GuestManagementService.Application.Seating.ApplyAssignmentsBatch;

public sealed class ApplyAssignmentsBatchCommandValidator : AbstractValidator<ApplyAssignmentsBatchCommand>
{
    public const int MaxOps = 200;

    public ApplyAssignmentsBatchCommandValidator()
    {
        RuleFor(command => command.EventId)
            .NotEmpty();

        RuleFor(command => command.Ops)
            .NotEmpty()
            .Must(ops => ops.Count <= MaxOps)
            .WithMessage($"A batch may contain at most {MaxOps} operations.");

        RuleForEach(command => command.Ops).ChildRules(op =>
        {
            op.RuleFor(o => o.GuestId)
                .NotEmpty();

            op.RuleFor(o => o.TableId)
                .NotNull()
                .When(o => o.Op == SeatingBatchOpType.Assign)
                .WithMessage("TableId is required for assign operations.");

            op.RuleFor(o => o.SeatIndex)
                .NotNull()
                .GreaterThanOrEqualTo(0)
                .When(o => o.Op == SeatingBatchOpType.Assign)
                .WithMessage("SeatIndex is required and must be non-negative for assign operations.");
        });
    }
}
