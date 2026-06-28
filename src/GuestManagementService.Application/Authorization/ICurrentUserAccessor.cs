namespace GuestManagementService.Application.Authorization;

public interface ICurrentUserAccessor
{
    CurrentUser? User { get; }
}
