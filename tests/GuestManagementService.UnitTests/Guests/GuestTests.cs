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
            null,
            now);

        Assert.Equal("Ada", guest.FirstName);
        Assert.Equal("Lovelace", guest.LastName);
        Assert.Equal("+1 555 123 4567", guest.PhoneNumber);
        Assert.Equal("+15551234567", guest.NormalizedPhoneNumber);
        Assert.Equal("ADA@EXAMPLE.COM", guest.EmailAddress);
        Assert.Equal("ada@example.com", guest.NormalizedEmailAddress);
        Assert.Equal(Gender.PreferNotToSay, guest.Gender);
        Assert.Equal("{\"relationship\":\"Family\"}", guest.Metadata);
        Assert.Empty(guest.Tags);
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
            null,
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
            null,
            DateTimeOffset.UtcNow);

        Assert.Null(guest.EmailAddress);
        Assert.Null(guest.NormalizedEmailAddress);
    }

    [Fact]
    public void Create_WhenTagsProvided_TrimsAndStoresThem()
    {
        var guest = CreateWithTags(new[] { "  College friends  ", "Head table" });

        Assert.Equal(new[] { "College friends", "Head table" }, guest.Tags);
    }

    [Fact]
    public void Create_WhenTagsHaveCaseInsensitiveDuplicates_KeepsFirstOnly()
    {
        var guest = CreateWithTags(new[] { "Family", "family", "FAMILY" });

        Assert.Equal(new[] { "Family" }, guest.Tags);
    }

    [Fact]
    public void Create_WhenTagsContainBlanks_DropsBlanks()
    {
        var guest = CreateWithTags(new[] { "Family", "", "   ", "Head table" });

        Assert.Equal(new[] { "Family", "Head table" }, guest.Tags);
    }

    [Fact]
    public void Create_WhenTagIsNull_TreatedAsBlank()
    {
        var guest = CreateWithTags(new[] { "Family", null!, "Head table" });

        Assert.Equal(new[] { "Family", "Head table" }, guest.Tags);
    }

    [Fact]
    public void Create_WhenTooManyTags_Throws()
    {
        var tags = Enumerable.Range(0, Guest.MaxTags + 1).Select(i => $"Tag{i}").ToArray();

        Assert.Throws<ArgumentException>(() => CreateWithTags(tags));
    }

    [Fact]
    public void Create_WhenTagTooLong_Throws()
    {
        var tags = new[] { new string('a', Guest.MaxTagLength + 1) };

        Assert.Throws<ArgumentException>(() => CreateWithTags(tags));
    }

    [Fact]
    public void Create_WhenTagsAreNull_ReturnsEmpty()
    {
        var guest = CreateWithTags(null);

        Assert.Empty(guest.Tags);
    }

    private static Guest CreateWithTags(IReadOnlyList<string>? tags)
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
            tags,
            DateTimeOffset.UtcNow);
    }
}
