using GuestManagementService.Api.Endpoints;
using GuestManagementService.Api.Middleware;
using GuestManagementService.Api.Observability;
using GuestManagementService.Api.Responses;
using GuestManagementService.Api.Security;
using GuestManagementService.Application;
using GuestManagementService.Application.Authorization;
using GuestManagementService.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// The Angular SPA calls the guest endpoints directly from the browser, so its origin(s)
// must be allowed for CORS. Configure via "Cors:AllowedOrigins"; defaults to the local dev SPA.
const string spaCorsPolicy = "SpaCorsPolicy";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:4200"];

builder.AddServiceObservability("guest-management-service");
builder.Services.AddCors(options =>
{
    options.AddPolicy(spaCorsPolicy, policy =>
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod());
});
builder.Services.AddApiAuthentication(builder.Configuration);
builder.Services.AddPermissionPolicies();
builder.Services.AddScoped<CurrentUserAccessor>();
builder.Services.AddScoped<ICurrentUserAccessor>(sp => sp.GetRequiredService<CurrentUserAccessor>());
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseFriendlyErrorResponses();
app.UseRequestLogging();
app.UseCors(spaCorsPolicy);
app.UseAuthentication();
app.UseCurrentUser();
app.UseAuthorization();

app.MapPingEndpoints();
app.MapGuestEndpoints();
app.MapSeatingEndpoints();

app.Run();
