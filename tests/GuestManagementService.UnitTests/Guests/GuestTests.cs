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
            DateTimeOffset.UtcNow);

        Assert.Null(guest.EmailAddress);
        Assert.Null(guest.NormalizedEmailAddress);
    }
}
