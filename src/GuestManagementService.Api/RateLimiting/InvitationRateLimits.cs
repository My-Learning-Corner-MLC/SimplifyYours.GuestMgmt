using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;

namespace GuestManagementService.Api.RateLimiting;

/// <summary>
/// Which set of limits applies to a request.
/// </summary>
public enum InvitationRateLimitKind
{
    /// <summary>Not a public invitation route — no limit applies.</summary>
    None = 0,

    /// <summary>Reading an invitation (JSON or rendered HTML).</summary>
    Read = 1,

    /// <summary>Submitting or editing an RSVP.</summary>
    Write = 2,
}

/// <summary>
/// Rate limits for the anonymous invitation endpoints.
/// </summary>
/// <remarks>
/// Two limits apply to every invitation request — one keyed on the invitation token, one on the
/// client IP — so they are composed with <see cref="PartitionedRateLimiter.CreateChained{T}"/>.
/// A single per-endpoint policy could only express one of the two.
/// <para>
/// The per-IP limits are DoS protection, not anti-enumeration: a 128-bit token space already makes
/// guessing infeasible, so they are set high enough that a shared office or household NAT does not
/// trip them.
/// </para>
/// </remarks>
public static class InvitationRateLimits
{
    public const string InvitationPathPrefix = "/guests/invitations/";

    public const int ReadPermitsPerToken = 30;
    public const int ReadPermitsPerIp = 300;
    public const int WritePermitsPerToken = 10;
    public const int WritePermitsPerIp = 60;

    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Classifies a request by path and method. Pure so the routing rules can be unit tested
    /// without standing up a server.
    /// </summary>
    public static InvitationRateLimitKind Classify(PathString path, string method)
    {
        if (!path.HasValue || !path.Value!.StartsWith(InvitationPathPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return InvitationRateLimitKind.None;
        }

        return HttpMethods.IsPost(method)
            ? InvitationRateLimitKind.Write
            : InvitationRateLimitKind.Read;
    }

    /// <summary>
    /// Extracts the invitation token from the path, or null when there is not one.
    /// </summary>
    public static string? ExtractToken(PathString path)
    {
        if (!path.HasValue || !path.Value!.StartsWith(InvitationPathPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var remainder = path.Value[InvitationPathPrefix.Length..];
        var separator = remainder.IndexOf('/');
        var token = separator >= 0 ? remainder[..separator] : remainder;

        return string.IsNullOrWhiteSpace(token) ? null : token;
    }

    /// <summary>
    /// Chained limiter: a request must have permits available in <em>both</em> the token partition
    /// and the IP partition to proceed.
    /// </summary>
    public static PartitionedRateLimiter<HttpContext> CreateLimiter()
    {
        return PartitionedRateLimiter.CreateChained(
            PartitionedRateLimiter.Create<HttpContext, string>(PartitionByToken),
            PartitionedRateLimiter.Create<HttpContext, string>(PartitionByIp));
    }

    private static RateLimitPartition<string> PartitionByToken(HttpContext context)
    {
        var kind = Classify(context.Request.Path, context.Request.Method);
        var token = ExtractToken(context.Request.Path);

        if (kind == InvitationRateLimitKind.None || token is null)
        {
            return RateLimitPartition.GetNoLimiter("none");
        }

        var permits = kind == InvitationRateLimitKind.Write ? WritePermitsPerToken : ReadPermitsPerToken;

        return Sliding($"token:{kind}:{token}", permits);
    }

    private static RateLimitPartition<string> PartitionByIp(HttpContext context)
    {
        var kind = Classify(context.Request.Path, context.Request.Method);

        if (kind == InvitationRateLimitKind.None)
        {
            return RateLimitPartition.GetNoLimiter("none");
        }

        // A null RemoteIpAddress (unix socket, some test hosts) collapses into one shared partition
        // rather than throwing — sharing a bucket is a far better failure than a 500.
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var permits = kind == InvitationRateLimitKind.Write ? WritePermitsPerIp : ReadPermitsPerIp;

        return Sliding($"ip:{kind}:{ip}", permits);
    }

    private static RateLimitPartition<string> Sliding(string key, int permitLimit)
    {
        return RateLimitPartition.GetSlidingWindowLimiter(
            key,
            _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = Window,
                SegmentsPerWindow = 6,
                QueueLimit = 0,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            });
    }
}
