using GuestManagementService.Application.Authorization;

namespace GuestManagementService.Api.Security;

internal sealed class CurrentUserAccessor : ICurrentUserAccessor
{
    public CurrentUser? User { get; private set; }

    public void SetUser(CurrentUser user) => User = user;
}
