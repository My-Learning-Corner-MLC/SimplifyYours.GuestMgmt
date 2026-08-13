namespace GuestManagementService.Application.Abstractions.Invitations;

public interface IInvitationRenderer
{
    /// <summary>
    /// Renders the complete HTML document served to the sandboxed iframe.
    /// </summary>
    /// <param name="fieldValues">
    /// The content the organiser composed, keyed by merge token. Supplied by the invitation
    /// settings, not by the replicated event record — an event edited after the invitation was
    /// written must not silently rewrite what guests were shown.
    /// </param>
    /// <param name="guestName">
    /// The one value that is never typed: it belongs to whichever guest's link is being opened.
    /// </param>
    /// <param name="eventType">Selects which merge tokens the template may resolve.</param>
    /// <remarks>
    /// Asynchronous purely so a render can be abandoned. Scriban honours a cancellation token
    /// during async evaluation only, so a synchronous signature would leave the wall-clock bound on
    /// this public, unauthenticated endpoint unenforceable.
    /// </remarks>
    Task<string> RenderAsync(
        IReadOnlyDictionary<string, string?> fieldValues,
        string guestName,
        string eventType);
}
