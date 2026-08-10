using GuestManagementService.Domain.EventReferences;
using GuestManagementService.Domain.Guests;
using MediatR;

namespace GuestManagementService.Application.Invitations.GetInvitation;

/// <summary>
/// Anonymous: the token is the only credential. Deliberately does not derive from BaseCommand —
/// there is no authenticated user on this path.
/// </summary>
public sealed record GetInvitationQuery(string Token) : IRequest<GetInvitationResult>;

public enum GetInvitationStatus
{
    Found = 0,
    NotFound = 1,
}

public sealed record GetInvitationResult(
    GetInvitationStatus Status,
    Guest? Guest = null,
    EventReference? Event = null,
    DateTimeOffset? Deadline = null,
    bool IsOpen = false)
{
    public static GetInvitationResult Found(
        Guest guest,
        EventReference eventReference,
        DateTimeOffset? deadline,
        bool isOpen) =>
        new(GetInvitationStatus.Found, guest, eventReference, deadline, isOpen);

    public static GetInvitationResult NotFound() => new(GetInvitationStatus.NotFound);
}
