using GuestManagementService.Application.Authorization;
using MediatR;

namespace GuestManagementService.Application.Seating.DeleteTable;

public sealed record DeleteTableCommand(Guid EventId, Guid TableId) : BaseCommand, IRequest<DeleteTableResult>;
