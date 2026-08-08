using GuestManagementService.Application.Authorization;
using MediatR;

namespace GuestManagementService.Application.Seating.UpdateAreaPosition;

public sealed record UpdateAreaPositionCommand(
    Guid EventId,
    Guid AreaId,
    double PositionX,
    double PositionY,
    double Rotation) : BaseCommand, IRequest<UpdateAreaPositionResult>;
