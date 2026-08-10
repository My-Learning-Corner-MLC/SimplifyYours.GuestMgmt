namespace GuestManagementService.Application.Abstractions.Invitations;

/// <summary>
/// The fixed merge-token allowlist a template may reference. Every value is plain text; the
/// renderer is responsible for escaping it.
/// </summary>
/// <remarks>
/// Venue is a single display string rather than separate name/address fields: templates lay it out
/// as one "Where" line, and splitting it would push formatting decisions into template markup.
/// </remarks>
public sealed record InvitationRenderModel(
    string GuestName,
    string EventName,
    string EventDate,
    string Venue);

public interface IInvitationRenderer
{
    /// <summary>Renders the complete HTML document served to the sandboxed iframe.</summary>
    string Render(InvitationRenderModel model);
}
