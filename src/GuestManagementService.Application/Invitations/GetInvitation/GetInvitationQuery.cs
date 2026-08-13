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
    bool IsOpen = false,
    /// <summary>
    /// The organiser's saved merge values. Carried on the result so the API layer never has to
    /// reach past MediatR to a repository to assemble a response.
    /// </summary>
    IReadOnlyDictionary<string, string?>? Content = null)
{
    public static GetInvitationResult Found(
        Guest guest,
        EventReference eventReference,
        DateTimeOffset? deadline,
        bool isOpen,
        IReadOnlyDictionary<string, string?> content) =>
        new(GetInvitationStatus.Found, guest, eventReference, deadline, isOpen, content);

    public static GetInvitationResult NotFound() => new(GetInvitationStatus.NotFound);
}
