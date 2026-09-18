using GuestManagementService.Application.Authorization;
using MediatR;

namespace GuestManagementService.Application.Seating.DeleteArea;

public sealed record DeleteAreaCommand(Guid EventId, Guid AreaId) : BaseCommand, IRequest<DeleteAreaResult>;
