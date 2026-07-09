using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace GuestManagementService.Api.Security;

public static class AuthorizationExtensions
{
    public static IServiceCollection AddPermissionPolicies(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            .AddPolicy(Permissions.GuestsAdd, policy =>
                policy.RequireClaim(Permissions.ClaimType, Permissions.GuestsAdd))
            .AddPolicy(Permissions.GuestsView, policy =>
                policy.RequireClaim(Permissions.ClaimType, Permissions.GuestsView));

        services.AddSingleton<IAuthorizationMiddlewareResultHandler, PermissionDeniedResultHandler>();

        return services;
    }
}
