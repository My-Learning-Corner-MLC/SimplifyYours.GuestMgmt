using GuestManagementService.Api.Endpoints;
using GuestManagementService.Application;
using GuestManagementService.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.MapPingEndpoints();
app.MapGuestEndpoints();

app.Run();
