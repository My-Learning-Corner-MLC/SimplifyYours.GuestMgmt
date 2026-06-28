using MediatR;

namespace GuestManagementService.Application.Authorization;

public sealed class CurrentUserPipelineBehavior<TRequest, TResponse>(
    ICurrentUserAccessor currentUserAccessor)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is BaseCommand baseCommand)
        {
            baseCommand.CurrentUser = currentUserAccessor.User
                ?? throw new InvalidOperationException(
                    "Current user was not resolved. Ensure CurrentUserMiddleware is registered before MediatR dispatch.");
        }

        return next();
    }
}
