using System.Security.Claims;
using GuestManagementService.Api.Security;
using GuestManagementService.Application.Authorization;

namespace GuestManagementService.Api.Middleware;

internal sealed class CurrentUserMiddleware(RequestDelegate next)
{
    private const string SubjectClaim = "sub";
    private const string TenantIdClaim = "tenant_id";

    public Task InvokeAsync(HttpContext context, ICurrentUserAccessor currentUserAccessor)
    {
        if (context.User?.Identity?.IsAuthenticated != true)
        {
            return next(context);
        }

        if (!TryResolve(context.User, out var currentUser))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        }

        ((CurrentUserAccessor)currentUserAccessor).User = currentUser;
        return next(context);
    }

    private static bool TryResolve(ClaimsPrincipal principal, out CurrentUser currentUser)
    {
        currentUser = default!;

        var subject = principal.FindFirstValue(SubjectClaim);
        if (!Guid.TryParse(subject, out var userId))
        {
            return false;
        }

        var tenant = principal.FindFirstValue(TenantIdClaim);
        if (!Guid.TryParse(tenant, out var tenantId))
        {
            return false;
        }

        currentUser = new CurrentUser(userId, tenantId);
        return true;
    }
}

internal static class CurrentUserMiddlewareExtensions
{
    public static IApplicationBuilder UseCurrentUser(this IApplicationBuilder app)
    {
        return app.UseMiddleware<CurrentUserMiddleware>();
    }
}
