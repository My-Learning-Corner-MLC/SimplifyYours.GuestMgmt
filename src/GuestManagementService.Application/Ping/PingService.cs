using GuestManagementService.Contracts.Ping;

namespace GuestManagementService.Application.Ping;

public sealed class PingService(TimeProvider timeProvider) : IPingService
{
    private const string ServiceUpMessage = "Guest Management service is up.";

    public PingStatusResponse GetStatus()
    {
        var currentGmtDateTime = timeProvider.GetUtcNow();

        return new PingStatusResponse(ServiceUpMessage, currentGmtDateTime);
    }
}
