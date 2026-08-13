using GuestManagementService.Api.Endpoints;
using GuestManagementService.Application.Abstractions.Invitations;
using GuestManagementService.Application.Invitations.GetInvitation;
using GuestManagementService.Contracts.Invitations;
using GuestManagementService.Domain.EventReferences;
using GuestManagementService.Domain.Guests;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Configuration;
using Moq;

namespace GuestManagementService.UnitTests.Invitations;

/// <summary>
/// The response headers on the public invitation surface.
/// </summary>
/// <remarks>
/// These headers are the whole mitigation for AC 8 and AC 14: the page names a real person and
/// gives an event's address, so a shared cache holding it or a crawler indexing it puts guest
/// details somewhere nobody agreed to. Nothing else in the suite runs at response level, so before
/// these tests every one of them could be deleted with a green build.
/// <para>
/// The endpoint methods are called directly rather than through a test server — they are static and
/// take their dependencies as parameters, so a <see cref="DefaultHttpContext"/> is enough to observe
/// what they write.
/// </para>
/// </remarks>
public sealed class InvitationEndpointsTests
{
    private const string SpaOrigin = "https://app.example.test";
    private static readonly Guid EventId = Guid.Parse("2c6f1a8e-9d3b-4f70-8a15-5b2e7c9d0f31");
    private static readonly DateTimeOffset Now = new(2026, 8, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task GetInvitation_TellsCachesAndCrawlersToKeepOut()
    {
        var httpContext = new DefaultHttpContext();

        await InvitationEndpoints.GetInvitationAsync(
            "tok-abc123",
            httpContext,
            Sender(Found()),
            CancellationToken.None);

        Assert.Equal("no-store", httpContext.Response.Headers.CacheControl);
        Assert.Equal("noindex, nofollow", httpContext.Response.Headers["X-Robots-Tag"]);
    }

    [Fact]
    public async Task RenderInvitation_NamesTheSpaOriginSoTheAppCanFrameIt()
    {
        // frame-ancestors must name the SPA, not deny framing: the invitation is delivered inside
        // the app's own iframe, so DENY would blank the page. This exact mismatch has already
        // broken the feature once, when the configured origin did not match where the SPA runs.
        var httpContext = new DefaultHttpContext();

        await InvitationEndpoints.RenderInvitationAsync(
            "tok-abc123",
            httpContext,
            Sender(Found()),
            Renderer(),
            Configuration(SpaOrigin),
            CancellationToken.None);

        Assert.Equal($"frame-ancestors {SpaOrigin}", httpContext.Response.Headers.ContentSecurityPolicy);
        Assert.Equal("no-store", httpContext.Response.Headers.CacheControl);
    }

    [Fact]
    public async Task RenderInvitation_FallsBackToSelfRatherThanEmittingAnEmptyDirective()
    {
        var httpContext = new DefaultHttpContext();

        await InvitationEndpoints.RenderInvitationAsync(
            "tok-abc123",
            httpContext,
            Sender(Found()),
            Renderer(),
            Configuration(origin: null),
            CancellationToken.None);

        // "frame-ancestors " with nothing after it is not a restriction, it is a malformed header.
        Assert.Equal("frame-ancestors 'self'", httpContext.Response.Headers.ContentSecurityPolicy);
    }

    [Fact]
    public async Task GetInvitation_SetsNoPublicHeadersOnAMiss()
    {
        // A 404 carries no guest data, and the error response has its own shape. What matters is
        // that the miss path returns the same not-found regardless of why it missed.
        var httpContext = new DefaultHttpContext();

        var result = await InvitationEndpoints.GetInvitationAsync(
            "tok-unknown",
            httpContext,
            Sender(GetInvitationResult.NotFound()),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.True(string.IsNullOrEmpty(httpContext.Response.Headers.CacheControl));
    }

    [Fact]
    public async Task GetInvitation_ReportsWhenTheGuestFirstAnswered()
    {
        // Prove-It: the response record had no RespondedAt at all, so the page's "You responded"
        // chip rendered without a date for every returning guest while storage held it correctly.
        var respondedAt = Now.AddDays(-3);
        var httpContext = new DefaultHttpContext();

        var result = await InvitationEndpoints.GetInvitationAsync(
            "tok-abc123",
            httpContext,
            Sender(Found(respondedAt)),
            CancellationToken.None);

        var response = Assert.IsType<GetInvitationResponse>(
            Assert.IsAssignableFrom<IValueHttpResult>(result).Value);

        Assert.Equal(respondedAt, response.Rsvp.RespondedAt);
    }

    private static ISender Sender(GetInvitationResult result)
    {
        var sender = new Mock<ISender>();
        sender
            .Setup(s => s.Send(It.IsAny<GetInvitationQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        return sender.Object;
    }

    private static IInvitationRenderer Renderer()
    {
        var renderer = new Mock<IInvitationRenderer>();
        renderer
            .Setup(r => r.RenderAsync(
                It.IsAny<IReadOnlyDictionary<string, string?>>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ReturnsAsync("<html><body>invitation</body></html>");

        return renderer.Object;
    }

    private static IConfiguration Configuration(string? origin)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Invitations:PublicBaseUrl"] = origin,
            })
            .Build();
    }

    private static GetInvitationResult Found(DateTimeOffset? respondedAt = null)
    {
        var guest = Guest.Create(
            Guid.NewGuid(),
            EventId,
            Guid.NewGuid(),
            "Ada",
            "Lovelace",
            "+15551234567",
            "+15551234567",
            "ada@example.com",
            "ada@example.com",
            Gender.PreferNotToSay,
            "{\"plusOnes\":2}",
            "tok-abc123",
            Now);

        if (respondedAt is not null)
        {
            guest.RecordRsvp(RsvpStatus.Accepted, guest.Metadata, respondedAt.Value);
        }

        var eventReference = EventReference.Active(
            EventId,
            "Ada's wedding",
            Guid.NewGuid(),
            Now,
            "wedding",
            new DateOnly(2026, 9, 12),
            timeZoneId: "UTC");

        return GetInvitationResult.Found(
            guest,
            eventReference,
            Now.AddDays(30),
            isOpen: true,
            new Dictionary<string, string?> { ["eventName"] = "Ada's wedding" });
    }
}
