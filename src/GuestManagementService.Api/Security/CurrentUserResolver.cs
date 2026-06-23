using System.Security.Claims;
using GuestManagementService.Application.Authorization;

namespace GuestManagementService.Api.Security;

internal static class CurrentUserResolver
{
    private const string SubjectClaim = "sub";
    private const string TenantIdClaim = "tenant_id";
    private const string PermissionsClaim = "permissions";

    public static bool TryResolve(ClaimsPrincipal principal, out CurrentUser currentUser)
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

        var permissions = principal.FindAll(PermissionsClaim).Select(c => c.Value).ToArray();

        currentUser = new CurrentUser(userId, tenantId, permissions);
        return true;
    }
}
