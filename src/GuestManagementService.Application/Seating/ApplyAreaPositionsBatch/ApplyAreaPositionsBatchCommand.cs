using GuestManagementService.Application.Authorization;
using MediatR;

namespace GuestManagementService.Application.Seating.ApplyAreaPositionsBatch;

public sealed record ApplyAreaPositionsBatchCommand(
    Guid EventId,
    IReadOnlyList<AreaPositionInput> Positions) : BaseCommand, IRequest<ApplyAreaPositionsBatchResult>;
