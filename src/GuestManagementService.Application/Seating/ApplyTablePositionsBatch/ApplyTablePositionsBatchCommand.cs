using GuestManagementService.Application.Authorization;
using MediatR;

namespace GuestManagementService.Application.Seating.ApplyTablePositionsBatch;

public sealed record ApplyTablePositionsBatchCommand(
    Guid EventId,
    IReadOnlyList<TablePositionInput> Positions) : BaseCommand, IRequest<ApplyTablePositionsBatchResult>;
