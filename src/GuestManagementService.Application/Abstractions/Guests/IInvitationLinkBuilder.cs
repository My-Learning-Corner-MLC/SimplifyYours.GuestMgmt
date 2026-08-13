namespace GuestManagementService.Application.Abstractions.Guests;

/// <summary>
/// Builds the public URL a guest opens for their invitation. The host comes from configuration
/// rather than the incoming request, so a spoofed Host header cannot make the service mint links
/// pointing at somewhere else.
/// </summary>
public interface IInvitationLinkBuilder
{
    string BuildUrl(string invitationToken);
}
