namespace GuestManagementService.Contracts.Ping;

public sealed record PingStatusResponse(
    string Message,
    DateTimeOffset CurrentGmtDateTime);
