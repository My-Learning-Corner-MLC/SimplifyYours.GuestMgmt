using GuestManagementService.Application.Authorization;
using MediatR;

namespace GuestManagementService.Application.Seating.UpdateTable;

public sealed record UpdateTableCommand(
    Guid EventId,
    Guid TableId,
    string? Name,
    string? Shape,
    int SeatCount,
    bool IsFull) : BaseCommand, IRequest<UpdateTableResult>;
