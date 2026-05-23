using GuestManagementService.Application.Ping;

namespace GuestManagementService.Api.Endpoints;

internal static class PingEndpoints
{
    public static IEndpointRouteBuilder MapPingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints
            .MapGet("/ping", (IPingService pingService, ILoggerFactory loggerFactory) =>
            {
                var response = pingService.GetStatus();

                loggerFactory
                    .CreateLogger("GuestManagementService.Ping")
                    .LogInformation(
                        "Guest Management service is up. Current GMT datetime: {CurrentGmtDateTime}",
                        response.CurrentGmtDateTime);

                return Results.Ok(response);
            })
            .WithName("Ping");

        return endpoints;
    }
}
