using GuestManagementService.Application.Authorization;
using MediatR;

namespace GuestManagementService.Application.Seating.UnassignSeat;

public sealed record UnassignSeatCommand(
    Guid EventId,
    Guid TableId,
    int SeatIndex) : BaseCommand, IRequest<UnassignSeatResult>;
