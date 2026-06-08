using GuestManagementService.Contracts.Ping;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GuestManagementService.Application.Ping;

public sealed class GetPingStatusQueryHandler(
    TimeProvider timeProvider,
    ILogger<GetPingStatusQueryHandler> logger)
    : IRequestHandler<GetPingStatusQuery, PingStatusResponse>
{
    private const string ServiceUpMessage = "Guest Management service is up.";

    public Task<PingStatusResponse> Handle(
        GetPingStatusQuery request,
        CancellationToken cancellationToken)
    {
        var currentGmtDateTime = timeProvider.GetUtcNow();
        var response = new PingStatusResponse(ServiceUpMessage, currentGmtDateTime);

        logger.LogInformation(
            "Guest Management ping status generated. CurrentGmtDateTime: {CurrentGmtDateTime}.",
            currentGmtDateTime);

        return Task.FromResult(response);
    }
}
