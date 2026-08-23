using GuestManagementService.Application.Abstractions.Guests;
using Microsoft.Extensions.Configuration;

namespace GuestManagementService.Infrastructure.Guests;

public sealed class InvitationLinkBuilder : IInvitationLinkBuilder
{
    private readonly string _baseUrl;

    public InvitationLinkBuilder(IConfiguration configuration)
    {
        var configured = configuration["Invitations:PublicBaseUrl"];

        if (string.IsNullOrWhiteSpace(configured))
        {
            throw new InvalidOperationException(
                "Configuration value 'Invitations:PublicBaseUrl' is required to build invitation links.");
        }

        _baseUrl = configured.TrimEnd('/');
    }

    public string BuildUrl(string invitationToken)
    {
        if (string.IsNullOrWhiteSpace(invitationToken))
        {
            throw new ArgumentException("Invitation token is required.", nameof(invitationToken));
        }

        return $"{_baseUrl}/invitation/{Uri.EscapeDataString(invitationToken)}";
    }
}
