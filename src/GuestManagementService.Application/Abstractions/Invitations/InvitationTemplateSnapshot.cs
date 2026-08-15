namespace GuestManagementService.Application.Abstractions.Invitations;

/// <summary>
/// The content <see cref="IInvitationRenderer"/> needs to produce a document — copied verbatim from
/// <c>EventInvitationSettings</c>, never fetched live. See that entity's remarks for why the
/// snapshot exists at all.
/// </summary>
public sealed record InvitationTemplateSnapshot(
    Guid TemplateId,
    int TemplateVersion,
    string HtmlContent,
    string? CssContent,
    string? JsContent);
