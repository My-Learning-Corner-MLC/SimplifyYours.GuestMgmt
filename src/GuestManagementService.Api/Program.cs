using GuestManagementService.Api.Endpoints;
using GuestManagementService.Api.Middleware;
using GuestManagementService.Api.Observability;
using GuestManagementService.Api.Responses;
using GuestManagementService.Api.Security;
using GuestManagementService.Application;
using GuestManagementService.Application.Authorization;
using GuestManagementService.Infrastructure;
using GuestManagementService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceObservability("guest-management-service");
builder.Services.AddApiAuthentication(builder.Configuration);
builder.Services.AddPermissionPolicies();
builder.Services.AddScoped<CurrentUserAccessor>();
builder.Services.AddScoped<ICurrentUserAccessor>(sp => sp.GetRequiredService<CurrentUserAccessor>());
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// Creates the database (if missing) and applies pending migrations. Idempotent,
// so safe on every startup -- needed because containerized environments have no
// separate "run dotnet ef database update" step before the app starts.
using (var migrationScope = app.Services.CreateScope())
{
    migrationScope.ServiceProvider.GetRequiredService<GuestManagementServiceDbContext>().Database.Migrate();
}

app.UseFriendlyErrorResponses();
app.UseRequestLogging();
app.UseAuthentication();
app.UseCurrentUser();
app.UseAuthorization();

app.MapPingEndpoints();
app.MapGuestEndpoints();

app.Run();
