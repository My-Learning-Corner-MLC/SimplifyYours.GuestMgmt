using MediatR;

namespace GuestManagementService.Application.Invitations.RenderInvitation;

/// <summary>
/// <c>GET /invitations/{token}/render</c> resolves one of two token types, each rendering the
/// event's committed invitation. Previewing a template — chosen or not yet chosen — is entirely
/// template-management-service's own concern; this handler never issues or resolves preview
/// tokens.
/// </summary>
public sealed record ResolveInvitationRenderQuery(string Token) : IRequest<ResolveInvitationRenderResult>;
