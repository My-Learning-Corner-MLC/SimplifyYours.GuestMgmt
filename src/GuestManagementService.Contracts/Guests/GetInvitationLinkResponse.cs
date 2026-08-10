namespace GuestManagementService.Contracts.Guests;

public sealed record GetInvitationLinkResponse(
    Guid GuestId,
    string InvitationToken,
    string InvitationUrl);
