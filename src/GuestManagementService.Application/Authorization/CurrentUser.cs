namespace GuestManagementService.Application.Authorization;

public sealed record CurrentUser(Guid UserId, Guid TenantId, IReadOnlyCollection<string> Permissions)
{
    public bool HasPermission(string permission)
    {
        return Permissions.Contains(permission, StringComparer.Ordinal);
    }
}
