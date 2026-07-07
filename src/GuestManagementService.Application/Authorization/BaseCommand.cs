namespace GuestManagementService.Application.Authorization;

public abstract record BaseCommand : IAuthenticatedRequest
{
    public CurrentUser CurrentUser { get; set; } = null!;
}
