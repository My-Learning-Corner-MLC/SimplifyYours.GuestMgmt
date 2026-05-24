using GuestManagementService.Contracts.Ping;
using MediatR;

namespace GuestManagementService.Application.Ping;

public sealed class GetPingStatusQueryHandler(TimeProvider timeProvider)
    : IRequestHandler<GetPingStatusQuery, PingStatusResponse>
{
    private const string ServiceUpMessage = "Guest Management service is up.";

    public Task<PingStatusResponse> Handle(
        GetPingStatusQuery request,
        CancellationToken cancellationToken)
    {
        var currentGmtDateTime = timeProvider.GetUtcNow();
        var response = new PingStatusResponse(ServiceUpMessage, currentGmtDateTime);

        return Task.FromResult(response);
    }
}
