namespace GuestManagementService.Application.Abstractions.Invitations;

/// <summary>
/// Every value a template may merge. Which of them a given template is actually allowed to
/// reference depends on the event type — see the renderer's allowlist.
/// </summary>
/// <remarks>
/// All values are plain text; the renderer is responsible for escaping them.
/// <para>
/// <see cref="BrideName"/> and <see cref="GroomName"/> have no source in the platform yet. Neither
/// event-service nor guest metadata holds couple names — guest metadata has <c>side</c>
/// (Bride/Groom), which is a different thing. They are allowlisted for wedding templates and
/// resolve to empty until a source exists.
/// </para>
/// </remarks>
public sealed record InvitationRenderModel(
    string GuestName,
    string EventName,
    string EventDate,
    string EventTime,
    string VenueName,
    string VenueAddress,
    string VenueNotes,
    string BrideName = "",
    string GroomName = "");

public interface IInvitationRenderer
{
    /// <summary>
    /// Renders the complete HTML document served to the sandboxed iframe. The event type selects
    /// which merge tokens the template is permitted to resolve.
    /// </summary>
    string Render(InvitationRenderModel model, string eventType);
}
