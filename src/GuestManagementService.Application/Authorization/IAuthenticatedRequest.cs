namespace GuestManagementService.Application.Authorization;

public interface IAuthenticatedRequest
{
    CurrentUser CurrentUser { get; set; }
}
