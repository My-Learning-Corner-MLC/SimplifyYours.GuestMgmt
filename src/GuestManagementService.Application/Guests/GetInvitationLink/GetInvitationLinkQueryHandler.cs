using GuestManagementService.Application.Abstractions.Guests;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GuestManagementService.Application.Guests.GetInvitationLink;

/// <summary>
/// Returns one guest's invitation link to the authenticated organiser who owns it.
/// </summary>
/// <remarks>
/// Deliberately a per-guest lookup rather than a field on the guest list. The list is paginated,
/// cached in the SPA, and the natural thing to log while debugging — returning every token there
/// would mean one captured response or one verbose log line exposes every live invitation for the
/// event at once. The copy action is a single deliberate click, so an extra request costs nothing
/// at the moment it matters.
/// </remarks>
public sealed class GetInvitationLinkQueryHandler(
    IGuestRepository guestRepository,
    IInvitationLinkBuilder invitationLinkBuilder,
    ILogger<GetInvitationLinkQueryHandler> logger)
    : IRequestHandler<GetInvitationLinkQuery, GetInvitationLinkResult>
{
    public async Task<GetInvitationLinkResult> Handle(
        GetInvitationLinkQuery request,
        CancellationToken cancellationToken)
    {
        var guest = await guestRepository.GetByIdAsync(
            request.GuestId,
            request.CurrentUser.TenantId,
            cancellationToken);

        if (guest is null)
        {
            // Tenant-scoped lookup, so a guest belonging to someone else is indistinguishable from
            // one that does not exist. No 403 — that would confirm the guest is real.
            logger.LogWarning(
                "Invitation link requested for a guest that is not available to the caller. GuestId: {GuestId}.",
                request.GuestId);

            return GetInvitationLinkResult.NotFound();
        }

        return GetInvitationLinkResult.Found(
            guest.Id,
            guest.InvitationToken,
            invitationLinkBuilder.BuildUrl(guest.InvitationToken));
    }
}
