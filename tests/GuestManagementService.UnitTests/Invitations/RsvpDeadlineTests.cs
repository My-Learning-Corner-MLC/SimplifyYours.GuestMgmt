using GuestManagementService.Application.Invitations;

namespace GuestManagementService.UnitTests.Invitations;

public sealed class RsvpDeadlineTests
{
    [Fact]
    public void Compute_IsEndOfTheDayBeforeTheEventInTheEventsZone()
    {
        // Event on 12 Sep in Ho Chi Minh City (UTC+7): RSVPs close at 23:59:59 local on 11 Sep,
        // which is 16:59:59 UTC. The guest's own location is irrelevant.
        var deadline = RsvpDeadline.Compute(new DateOnly(2026, 9, 12), "Asia/Ho_Chi_Minh");

        Assert.Equal(new DateTimeOffset(2026, 9, 11, 16, 59, 59, TimeSpan.Zero), deadline);
    }

    [Fact]
    public void Compute_UsesTheEventsZoneNotUtc()
    {
        var hcmc = RsvpDeadline.Compute(new DateOnly(2026, 9, 12), "Asia/Ho_Chi_Minh");
        var newYork = RsvpDeadline.Compute(new DateOnly(2026, 9, 12), "America/New_York");

        Assert.NotEqual(hcmc, newYork);
        Assert.True(hcmc < newYork, "A UTC+7 event should close earlier in absolute time than a UTC-4 one.");
    }

    [Fact]
    public void Compute_HandlesADaylightSavingBoundary()
    {
        // 2026-03-08 is the US spring-forward date. The deadline falls on 7 Mar, still EST (UTC-5),
        // so this pins the offset actually in force on the deadline day rather than on event day.
        var deadline = RsvpDeadline.Compute(new DateOnly(2026, 3, 8), "America/New_York");

        Assert.Equal(new DateTimeOffset(2026, 3, 8, 4, 59, 59, TimeSpan.Zero), deadline);
    }

    [Fact]
    public void Compute_FallsBackToUtcWhenTheZoneIsMissing()
    {
        Assert.Equal(
            new DateTimeOffset(2026, 9, 11, 23, 59, 59, TimeSpan.Zero),
            RsvpDeadline.Compute(new DateOnly(2026, 9, 12), null));
    }

    [Fact]
    public void Compute_FallsBackToUtcWhenTheZoneIsUnknownToThisHost()
    {
        // Reference data replicated from another service can name a zone this host does not know.
        // Degrading to UTC keeps the invitation readable; throwing would 500 the whole page.
        Assert.Equal(
            new DateTimeOffset(2026, 9, 11, 23, 59, 59, TimeSpan.Zero),
            RsvpDeadline.Compute(new DateOnly(2026, 9, 12), "Not/AZone"));
    }

    [Fact]
    public void Compute_ReturnsNullWhenTheEventHasNoDate()
    {
        Assert.Null(RsvpDeadline.Compute(null, "Asia/Ho_Chi_Minh"));
    }

    [Theory]
    [InlineData("2026-09-11T16:59:58Z", true)]
    [InlineData("2026-09-11T16:59:59Z", true)]
    [InlineData("2026-09-11T17:00:00Z", false)]
    public void IsOpen_ClosesExactlyAtTheDeadline(string now, bool expected)
    {
        Assert.Equal(
            expected,
            RsvpDeadline.IsOpen(new DateOnly(2026, 9, 12), "Asia/Ho_Chi_Minh", DateTimeOffset.Parse(now)));
    }

    [Fact]
    public void IsOpen_StaysOpenWhenThereIsNoDate()
    {
        Assert.True(RsvpDeadline.IsOpen(null, null, DateTimeOffset.UtcNow));
    }
}
