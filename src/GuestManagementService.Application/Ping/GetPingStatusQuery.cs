using GuestManagementService.Contracts.Ping;
using MediatR;

namespace GuestManagementService.Application.Ping;

public sealed record GetPingStatusQuery : IRequest<PingStatusResponse>;
