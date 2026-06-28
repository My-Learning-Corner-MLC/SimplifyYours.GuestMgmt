using GuestManagementService.Application.Authorization;

namespace GuestManagementService.Api.Security;

internal sealed class CurrentUserAccessor : ICurrentUserAccessor
{
    public CurrentUser? User { get; set; }
}
