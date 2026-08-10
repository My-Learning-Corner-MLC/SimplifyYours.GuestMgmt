using GuestManagementService.Domain.Guests;

namespace GuestManagementService.UnitTests.Guests;

public sealed class GuestTests
{
    [Fact]
    public void Create_NormalizesGuestFields()
    {
        var now = new DateTimeOffset(2026, 5, 24, 10, 0, 0, TimeSpan.Zero);
        var guest = Guest.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            " Ada ",
            " Lovelace ",
            " +1 555 123 4567 ",
            "+15551234567",
            " ADA@EXAMPLE.COM ",
            "ada@example.com",
            Gender.PreferNotToSay,
            "  {\"relationship\":\"Family\"}  ",
            "tok-test-invitation-token",
            now);

        Assert.Equal("Ada", guest.FirstName);
        Assert.Equal("Lovelace", guest.LastName);
        Assert.Equal("+1 555 123 4567", guest.PhoneNumber);
        Assert.Equal("+15551234567", guest.NormalizedPhoneNumber);
        Assert.Equal("ADA@EXAMPLE.COM", guest.EmailAddress);
        Assert.Equal("ada@example.com", guest.NormalizedEmailAddress);
        Assert.Equal(Gender.PreferNotToSay, guest.Gender);
        Assert.Equal("{\"relationship\":\"Family\"}", guest.Metadata);
        Assert.Equal(now, guest.CreatedAt);
    }

    [Fact]
    public void Create_WhenMetadataIsWhitespace_StoresNullMetadata()
    {
        var guest = Guest.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Ada",
            "Lovelace",
            "+15551234567",
            "+15551234567",
            "ada@example.com",
            "ada@example.com",
            Gender.PreferNotToSay,
            "   ",
            "tok-test-invitation-token",
            DateTimeOffset.UtcNow);

        Assert.Null(guest.Metadata);
    }

    [Fact]
    public void Create_WhenEmailIsWhitespace_StoresNullEmail()
    {
        var guest = Guest.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Ada",
            "Lovelace",
            "+15551234567",
            "+15551234567",
            " ",
            null,
            Gender.Other,
            null,
            "tok-test-invitation-token",
            DateTimeOffset.UtcNow);

        Assert.Null(guest.EmailAddress);
        Assert.Null(guest.NormalizedEmailAddress);
    }

    [Fact]
    public void Create_StoresTheInvitationTokenAndDefaultsBothStatuses()
    {
        var guest = NewGuest("tok-abc123");

        Assert.Equal("tok-abc123", guest.InvitationToken);
        Assert.Equal(DeliveryStatus.NotSent, guest.DeliveryStatus);
        Assert.Equal(RsvpStatus.NoResponse, guest.RsvpStatus);
        Assert.Null(guest.RespondedAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_RejectsABlankInvitationToken(string token)
    {
        // A guest without a token has no reachable invitation, so this must fail loudly at
        // construction rather than produce a silently broken row.
        Assert.Throws<ArgumentException>(() => NewGuest(token));
    }

    [Fact]
    public void Create_TrimsTheInvitationToken()
    {
        Assert.Equal("tok-abc123", NewGuest("  tok-abc123  ").InvitationToken);
    }

    private static Guest NewGuest(string invitationToken)
    {
        return Guest.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Ada",
            "Lovelace",
            "+15551234567",
            "+15551234567",
            "ada@example.com",
            "ada@example.com",
            Gender.PreferNotToSay,
            null,
            invitationToken,
            new DateTimeOffset(2026, 5, 24, 10, 0, 0, TimeSpan.Zero));
    }
}
