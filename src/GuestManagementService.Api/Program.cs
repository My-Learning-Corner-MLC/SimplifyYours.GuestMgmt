using GuestManagementService.Api.Endpoints;
using GuestManagementService.Api.Middleware;
using GuestManagementService.Api.Observability;
using GuestManagementService.Api.Responses;
using GuestManagementService.Application;
using GuestManagementService.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceObservability("guest-management-service");
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseFriendlyErrorResponses();
app.UseRequestLogging();

app.MapPingEndpoints();

app.Run();
