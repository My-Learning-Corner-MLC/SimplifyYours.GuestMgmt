namespace GuestManagementService.Application.Abstractions.Invitations;

/// <summary>
/// The merge values an invitation template can reference. Every value is plain text; the renderer
/// is responsible for escaping it.
/// </summary>
public sealed record InvitationRenderModel(
    string GuestName,
    string EventName,
    string EventDate,
    string EventTime,
    string VenueName,
    string VenueAddress,
    string EventDescription);

public interface IInvitationRenderer
{
    /// <summary>Renders a complete HTML document for the sandboxed iframe.</summary>
    string Render(InvitationRenderModel model);
}
