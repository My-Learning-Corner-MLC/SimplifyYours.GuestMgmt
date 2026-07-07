namespace GuestManagementService.Application.Authorization;

public sealed record CurrentUser(Guid UserId, Guid TenantId);
