namespace GuestManagementService.Application.Authorization;

public abstract record BaseCommand
{
    public CurrentUser CurrentUser { get; set; } = null!;
}
