using GuestManagementService.Application.Authorization;
using MediatR;

namespace GuestManagementService.Application.Guests.GetInvitationLink;

public sealed record GetInvitationLinkQuery(Guid GuestId) : BaseCommand, IRequest<GetInvitationLinkResult>;

public enum GetInvitationLinkStatus
{
    Found = 0,
    NotFound = 1,
}

public sealed record GetInvitationLinkResult(
    GetInvitationLinkStatus Status,
    Guid GuestId = default,
    string? InvitationToken = null,
    string? InvitationUrl = null)
{
    public static GetInvitationLinkResult Found(Guid guestId, string token, string url) =>
        new(GetInvitationLinkStatus.Found, guestId, token, url);

    public static GetInvitationLinkResult NotFound() => new(GetInvitationLinkStatus.NotFound);
}
