using GuestManagementService.Application.Ping;
using MediatR;

namespace GuestManagementService.Api.Endpoints;

internal static class PingEndpoints
{
    public static IEndpointRouteBuilder MapPingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints
            .MapGet("/ping", async (
                ISender sender,
                ILoggerFactory loggerFactory,
                CancellationToken cancellationToken) =>
            {
                var response = await sender.Send(new GetPingStatusQuery(), cancellationToken);

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
