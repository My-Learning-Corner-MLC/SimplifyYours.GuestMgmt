namespace GuestManagementService.Application.Authorization;

public interface ICurrentUserAccessor
{
    CurrentUser? User { get; }
    void SetUser(CurrentUser user);
}
