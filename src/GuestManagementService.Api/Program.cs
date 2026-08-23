using GuestManagementService.Api.Endpoints;
using GuestManagementService.Api.Middleware;
using GuestManagementService.Api.Observability;
using GuestManagementService.Api.RateLimiting;
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
builder.Services.AddGatewayForwardedHeaders(builder.Configuration);
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = InvitationRateLimits.CreateLimiter();
});

var app = builder.Build();

// Creates the database (if missing) and applies pending migrations. Idempotent,
// so safe on every startup -- needed because containerized environments have no
// separate "run dotnet ef database update" step before the app starts.
using (var migrationScope = app.Services.CreateScope())
{
    migrationScope.ServiceProvider.GetRequiredService<GuestManagementServiceDbContext>().Database.Migrate();
}

// Must come first: everything downstream that reads the client IP — request logging and, above all,
// the per-IP invitation rate limits — would otherwise see the gateway's address for every caller and
// share a single rate-limit partition across all guests.
app.UseForwardedHeaders();

app.UseFriendlyErrorResponses();
app.UseRequestLogging();
app.UseAuthentication();
app.UseCurrentUser();
app.UseAuthorization();

// After authorization so an authenticated caller's request is still counted, but before endpoint
// execution so a throttled request never reaches a handler or the database.
app.UseRateLimiter();

app.MapPingEndpoints();
app.MapGuestEndpoints();
app.MapInvitationEndpoints();
app.MapInvitationSettingsEndpoints();

app.Run();
