using GuestManagementService.Domain.EventReferences;
using GuestManagementService.Domain.Guests;
using MediatR;

namespace GuestManagementService.Application.Invitations.SubmitRsvp;

/// <summary>
/// Anonymous: the invitation token is the only credential. Deliberately not a BaseCommand — there
/// is no authenticated user on this path.
/// </summary>
public sealed record SubmitRsvpCommand(
    string Token,
    string? RsvpStatus,
    int? PlusOnesConfirmed,
    string? DietaryNotes) : IRequest<SubmitRsvpResult>;

public enum SubmitRsvpStatus
{
    Accepted = 0,

    /// <summary>Unknown token, or the event behind it is gone. Indistinguishable by design.</summary>
    NotFound = 1,

    /// <summary>Past the deadline — applies to edits as well as first submissions.</summary>
    Closed = 2,
}

public sealed record SubmitRsvpResult(
    SubmitRsvpStatus Status,
    Guest? Guest = null,
    EventReference? Event = null,
    DateTimeOffset? Deadline = null,
    /// <summary>
    /// The organiser's saved merge values, so the confirmation response can be built without the
    /// API layer loading them from a repository itself.
    /// </summary>
    IReadOnlyDictionary<string, string?>? Content = null)
{
    public static SubmitRsvpResult Accepted(
        Guest guest,
        EventReference eventReference,
        DateTimeOffset? deadline,
        IReadOnlyDictionary<string, string?> content) =>
        new(SubmitRsvpStatus.Accepted, guest, eventReference, deadline, content);

    public static SubmitRsvpResult NotFound() => new(SubmitRsvpStatus.NotFound);

    public static SubmitRsvpResult Closed() => new(SubmitRsvpStatus.Closed);
}
