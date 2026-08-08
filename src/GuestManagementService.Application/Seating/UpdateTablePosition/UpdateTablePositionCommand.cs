using GuestManagementService.Application.Authorization;
using MediatR;

namespace GuestManagementService.Application.Seating.UpdateTablePosition;

public sealed record UpdateTablePositionCommand(
    Guid EventId,
    Guid TableId,
    double PositionX,
    double PositionY,
    double Rotation) : BaseCommand, IRequest<UpdateTablePositionResult>;
