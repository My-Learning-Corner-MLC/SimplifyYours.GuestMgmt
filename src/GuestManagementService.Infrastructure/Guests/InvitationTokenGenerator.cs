using System.Security.Cryptography;
using GuestManagementService.Application.Abstractions.Guests;

namespace GuestManagementService.Infrastructure.Guests;

/// <summary>
/// 128 bits of cryptographic randomness, base64url-encoded without padding (22 characters).
/// Base64url keeps the token safe to drop straight into a URL path with no escaping.
/// </summary>
public sealed class InvitationTokenGenerator : IInvitationTokenGenerator
{
    internal const int TokenBytes = 16;

    public string Generate()
    {
        var buffer = RandomNumberGenerator.GetBytes(TokenBytes);

        return Convert.ToBase64String(buffer)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
