namespace GuestManagementService.Application.Invitations.RotatePublicToken;

public sealed record RotatePublicTokenResult(RotatePublicTokenStatus Status, string? PublicEventToken = null)
{
    public static RotatePublicTokenResult EventNotFound() => new(RotatePublicTokenStatus.EventNotFound);
}
