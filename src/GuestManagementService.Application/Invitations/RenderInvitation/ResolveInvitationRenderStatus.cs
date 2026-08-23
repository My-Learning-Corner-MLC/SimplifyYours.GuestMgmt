namespace GuestManagementService.Application.Invitations.RenderInvitation;

public enum ResolveInvitationRenderStatus
{
    /// <summary>Unknown or revoked token of either type — deliberately uniform.</summary>
    NotFound = 0,

    /// <summary>Guest token — slice 1's unchanged behaviour.</summary>
    Guest = 1,

    /// <summary>Public event token — guestName unbound.</summary>
    PublicEvent = 2,
}
