using GuestManagementService.Application.Authorization;
using MediatR;

namespace GuestManagementService.Application.Seating.AssignSeat;

public sealed record AssignSeatCommand(
    Guid EventId,
    Guid TableId,
    int SeatIndex,
    Guid GuestId) : BaseCommand, IRequest<AssignSeatResult>;
