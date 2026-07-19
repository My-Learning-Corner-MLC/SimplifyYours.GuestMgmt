using GuestManagementService.Application.Seating;
using GuestManagementService.Application.Seating.ApplyAssignmentsBatch;

namespace GuestManagementService.UnitTests.Seating.ApplyAssignmentsBatch;

public sealed class ApplyAssignmentsBatchCommandValidatorTests
{
    private readonly ApplyAssignmentsBatchCommandValidator validator = new();

    [Fact]
    public void Validate_WhenAssignOpIsWellFormed_HasNoErrors()
    {
        var ops = new[] { new SeatingBatchOpInput(SeatingBatchOpType.Assign, Guid.NewGuid(), Guid.NewGuid(), 0) };

        var result = validator.Validate(new ApplyAssignmentsBatchCommand(Guid.NewGuid(), ops));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WhenUnassignOpOmitsTableAndSeat_HasNoErrors()
    {
        var ops = new[] { new SeatingBatchOpInput(SeatingBatchOpType.Unassign, Guid.NewGuid(), null, null) };

        var result = validator.Validate(new ApplyAssignmentsBatchCommand(Guid.NewGuid(), ops));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WhenEventIdEmpty_HasError()
    {
        var ops = new[] { new SeatingBatchOpInput(SeatingBatchOpType.Unassign, Guid.NewGuid(), null, null) };

        var result = validator.Validate(new ApplyAssignmentsBatchCommand(Guid.Empty, ops));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_WhenOpsEmpty_HasError()
    {
        var result = validator.Validate(new ApplyAssignmentsBatchCommand(Guid.NewGuid(), []));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_WhenOpsExceedMax_HasError()
    {
        var ops = Enumerable.Range(0, ApplyAssignmentsBatchCommandValidator.MaxOps + 1)
            .Select(_ => new SeatingBatchOpInput(SeatingBatchOpType.Unassign, Guid.NewGuid(), null, null))
            .ToList();

        var result = validator.Validate(new ApplyAssignmentsBatchCommand(Guid.NewGuid(), ops));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_WhenAssignOpMissingTableId_HasError()
    {
        var ops = new[] { new SeatingBatchOpInput(SeatingBatchOpType.Assign, Guid.NewGuid(), null, 0) };

        var result = validator.Validate(new ApplyAssignmentsBatchCommand(Guid.NewGuid(), ops));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_WhenAssignOpMissingSeatIndex_HasError()
    {
        var ops = new[] { new SeatingBatchOpInput(SeatingBatchOpType.Assign, Guid.NewGuid(), Guid.NewGuid(), null) };

        var result = validator.Validate(new ApplyAssignmentsBatchCommand(Guid.NewGuid(), ops));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_WhenAssignOpSeatIndexNegative_HasError()
    {
        var ops = new[] { new SeatingBatchOpInput(SeatingBatchOpType.Assign, Guid.NewGuid(), Guid.NewGuid(), -1) };

        var result = validator.Validate(new ApplyAssignmentsBatchCommand(Guid.NewGuid(), ops));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_WhenGuestIdEmpty_HasError()
    {
        var ops = new[] { new SeatingBatchOpInput(SeatingBatchOpType.Unassign, Guid.Empty, null, null) };

        var result = validator.Validate(new ApplyAssignmentsBatchCommand(Guid.NewGuid(), ops));

        Assert.False(result.IsValid);
    }
}
