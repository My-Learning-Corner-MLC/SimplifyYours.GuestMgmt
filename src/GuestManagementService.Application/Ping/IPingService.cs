using GuestManagementService.Contracts.Ping;

namespace GuestManagementService.Application.Ping;

public interface IPingService
{
    PingStatusResponse GetStatus();
}
