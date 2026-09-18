using GuestManagementService.Application.Authorization;
using MediatR;

namespace GuestManagementService.Application.Seating.CreateTables;

public sealed record CreateTablesCommand(
    Guid EventId,
    string? Name,
    string? Shape,
    int SeatCount,
    int Count = 1) : BaseCommand, IRequest<CreateTablesResult>;
