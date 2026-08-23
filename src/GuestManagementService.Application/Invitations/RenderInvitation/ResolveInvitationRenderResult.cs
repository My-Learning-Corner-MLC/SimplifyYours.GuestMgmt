using GuestManagementService.Application.Abstractions.Invitations;

namespace GuestManagementService.Application.Invitations.RenderInvitation;

public sealed record ResolveInvitationRenderResult(
    ResolveInvitationRenderStatus Status,
    string EventType = "",
    /// <summary>Null means "omit guestName from the render model entirely" — see B5.</summary>
    string? GuestName = null,
    IReadOnlyDictionary<string, string?>? FieldValues = null,
    InvitationTemplateSnapshot? Snapshot = null)
{
    public static ResolveInvitationRenderResult NotFound() => new(ResolveInvitationRenderStatus.NotFound);
}
