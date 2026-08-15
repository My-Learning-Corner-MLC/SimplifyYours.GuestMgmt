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
    public const string InvitationPathPrefix = "/invitations/";

    /// <summary>
    /// Authenticated organiser routes now live under the same <c>/invitations/</c> prefix as the
    /// anonymous ones (<c>/invitations/events/{eventId}/...</c>,
    /// <c>/invitations/guests/{guestId}/link</c>). Neither "events" nor "guests" can ever be a real
    /// invitation token, so both are reserved first-segment literals this limiter must not mistake
    /// for one. Getting this wrong would not be a security hole — it is a throttling misclassification
    /// — but it is a real functional bug: every organiser's settings calls would share one rate-limit
    /// bucket keyed on the literal "events" or "guests", throttling them against each other instead of
    /// individually.
    /// </summary>
    private static readonly HashSet<string> ReservedFirstSegments =
        new(StringComparer.OrdinalIgnoreCase) { "events", "guests" };

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
        if (ExtractToken(path) is null)
        {
            return InvitationRateLimitKind.None;
        }

        return HttpMethods.IsPost(method)
            ? InvitationRateLimitKind.Write
            : InvitationRateLimitKind.Read;
    }

    /// <summary>
    /// Extracts the invitation token from the path, or null when there is not one — including when
    /// the first segment is a reserved organiser-route literal rather than a token.
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

        if (string.IsNullOrWhiteSpace(token) || ReservedFirstSegments.Contains(token))
        {
            return null;
        }

        return token;
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
