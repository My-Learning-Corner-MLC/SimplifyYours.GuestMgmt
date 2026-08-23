using System.Net;
using Microsoft.AspNetCore.HttpOverrides;

namespace GuestManagementService.Api.Security;

/// <summary>
/// Teaches the service to read the real client IP from <c>X-Forwarded-For</c>.
/// </summary>
/// <remarks>
/// Without this, <c>HttpContext.Connection.RemoteIpAddress</c> is the API gateway's address for
/// every caller, because the gateway terminates the connection. The per-IP invitation rate limits
/// then share one partition across all guests worldwide, which turns a DoS countermeasure into a
/// bucket any anonymous caller can exhaust to throttle everyone else.
/// <para>
/// The trust boundary is the whole point. <c>X-Forwarded-For</c> is a client-supplied header, so
/// honouring it from an untrusted peer is strictly worse than ignoring it — a caller could then
/// forge a fresh IP per request and evade the limit entirely. Only hops inside
/// <see cref="TrustedProxyNetworksKey"/> are believed, and <c>ForwardLimit = 1</c> means exactly one
/// hop (our gateway) is unwound, never a chain the client prepended to.
/// </para>
/// <para>
/// The default trusts the RFC 1918 private ranges plus loopback, which is where the gateway sits in
/// both the local compose stack and any container network. That is safe only while the service is
/// unreachable except through the gateway. If it is ever published directly to the internet, set
/// <c>Invitations:TrustedProxyNetworks</c> to the gateway's address alone.
/// </para>
/// </remarks>
public static class ForwardedHeadersSetup
{
    public const string TrustedProxyNetworksKey = "Invitations:TrustedProxyNetworks";

    /// <summary>Private ranges plus loopback — where a reverse proxy lives in this deployment.</summary>
    private static readonly string[] DefaultTrustedNetworks =
    [
        "127.0.0.0/8",
        "::1/128",
        "10.0.0.0/8",
        "172.16.0.0/12",
        "192.168.0.0/16",
    ];

    public static IServiceCollection AddGatewayForwardedHeaders(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var configured = configuration.GetSection(TrustedProxyNetworksKey).Get<string[]>();
        var networks = configured is { Length: > 0 } ? configured : DefaultTrustedNetworks;

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

            // One hop: the gateway. Anything further left in the header was put there by the client.
            options.ForwardLimit = 1;

            // The defaults trust loopback only, which would silently keep the gateway's own address
            // as the client IP in every containerized deployment.
            options.KnownProxies.Clear();
            options.KnownIPNetworks.Clear();

            foreach (var network in networks)
            {
                options.KnownIPNetworks.Add(Parse(network));
            }
        });

        return services;
    }

    /// <summary>
    /// Parses CIDR notation. Throws rather than skipping a malformed entry: a typo that silently
    /// dropped a trusted network would restore the shared-bucket bug with nothing to show for it.
    /// </summary>
    private static System.Net.IPNetwork Parse(string cidr)
    {
        var parts = cidr.Split('/', 2);

        if (parts.Length != 2
            || !IPAddress.TryParse(parts[0], out var prefix)
            || !int.TryParse(parts[1], out var length))
        {
            throw new InvalidOperationException(
                $"'{cidr}' in {TrustedProxyNetworksKey} is not valid CIDR notation (for example 10.0.0.0/8).");
        }

        return new System.Net.IPNetwork(prefix, length);
    }
}
