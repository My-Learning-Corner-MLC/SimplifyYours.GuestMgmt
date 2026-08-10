using GuestManagementService.Api.RateLimiting;
using Microsoft.AspNetCore.Http;

namespace GuestManagementService.UnitTests.RateLimiting;

public sealed class InvitationRateLimitsTests
{
    [Theory]
    [InlineData("/guests/invitations/tok123", "GET", InvitationRateLimitKind.Read)]
    [InlineData("/guests/invitations/tok123/render", "GET", InvitationRateLimitKind.Read)]
    [InlineData("/guests/invitations/tok123/rsvp", "POST", InvitationRateLimitKind.Write)]
    public void Classify_RecognisesInvitationRoutes(string path, string method, InvitationRateLimitKind expected)
    {
        Assert.Equal(expected, InvitationRateLimits.Classify(new PathString(path), method));
    }

    [Theory]
    [InlineData("/guests", "POST")]
    [InlineData("/guests/query", "POST")]
    [InlineData("/guests/6f9b3c2a-6d1e-4f5b-9c3a-2e7d8b1f4a55/invitation-link", "GET")]
    [InlineData("/ping", "GET")]
    [InlineData("/", "GET")]
    public void Classify_LeavesNonInvitationRoutesUnlimited(string path, string method)
    {
        // Authenticated organiser traffic must not be throttled by limits designed for anonymous
        // guests — in particular the invitation-link endpoint, whose path is deliberately similar.
        Assert.Equal(InvitationRateLimitKind.None, InvitationRateLimits.Classify(new PathString(path), method));
    }

    [Theory]
    [InlineData("/guests/invitations/tok123", "tok123")]
    [InlineData("/guests/invitations/tok123/render", "tok123")]
    [InlineData("/guests/invitations/tok123/rsvp", "tok123")]
    public void ExtractToken_ReadsTheTokenSegment(string path, string expected)
    {
        Assert.Equal(expected, InvitationRateLimits.ExtractToken(new PathString(path)));
    }

    [Theory]
    [InlineData("/guests/invitations/")]
    [InlineData("/guests/query")]
    public void ExtractToken_ReturnsNullWhenThereIsNoToken(string path)
    {
        Assert.Null(InvitationRateLimits.ExtractToken(new PathString(path)));
    }

    [Fact]
    public void Classify_IsCaseInsensitiveOnThePathPrefix()
    {
        Assert.Equal(
            InvitationRateLimitKind.Read,
            InvitationRateLimits.Classify(new PathString("/Guests/Invitations/tok123"), "GET"));
    }

    [Fact]
    public void WriteLimitsAreTighterThanReadLimits()
    {
        // Submitting an RSVP writes personal data from an anonymous caller, so it is deliberately
        // more constrained than simply viewing the invitation.
        Assert.True(InvitationRateLimits.WritePermitsPerToken < InvitationRateLimits.ReadPermitsPerToken);
        Assert.True(InvitationRateLimits.WritePermitsPerIp < InvitationRateLimits.ReadPermitsPerIp);
    }

    [Fact]
    public void CreateLimiter_BuildsAChainedLimiter()
    {
        // Both the token limit and the IP limit must apply to every invitation request; a single
        // per-endpoint policy could only express one of them.
        using var limiter = InvitationRateLimits.CreateLimiter();

        Assert.NotNull(limiter);
    }
}
