using GuestManagementService.Application.Authorization;
using MediatR;

namespace GuestManagementService.Application.Seating.ApplyAssignmentsBatch;

public sealed record ApplyAssignmentsBatchCommand(
    Guid EventId,
    IReadOnlyList<SeatingBatchOpInput> Ops) : BaseCommand, IRequest<ApplyAssignmentsBatchResult>;
