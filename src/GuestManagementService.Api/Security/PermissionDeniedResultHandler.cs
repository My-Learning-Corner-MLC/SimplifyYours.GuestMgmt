using GuestManagementService.Api.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace GuestManagementService.Api.Security;

internal sealed class PermissionDeniedResultHandler : IAuthorizationMiddlewareResultHandler
{
    private const string PermissionDeniedMessage = "You do not have permission to perform this action.";

    private readonly AuthorizationMiddlewareResultHandler _defaultHandler = new();

    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        if (authorizeResult.Forbidden && !authorizeResult.Challenged)
        {
            var result = ApiErrorResults.Forbidden(PermissionDeniedMessage, context);
            await result.ExecuteAsync(context);
            return;
        }

        await _defaultHandler.HandleAsync(next, context, policy, authorizeResult);
    }
}
