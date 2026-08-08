using GuestManagementService.Application.Authorization;
using MediatR;

namespace GuestManagementService.Application.Seating.GetSeatingLayout;

public sealed record GetSeatingLayoutQuery(Guid EventId) : BaseCommand, IRequest<GetSeatingLayoutResult>;
