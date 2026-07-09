using GuestManagementService.Application.Guests.Wedding;
using GuestManagementService.Domain.Guests.Wedding;

namespace GuestManagementService.UnitTests.Guests.Wedding;

public sealed class WeddingGuestMetadataMapperTests
{
    [Theory]
    [InlineData("Family", Relationship.Family)]
    [InlineData("friend", Relationship.Friend)]
    [InlineData("  Colleague  ", Relationship.Colleague)]
    public void TryParseRelationship_WhenKnown_ReturnsValue(string value, Relationship expected)
    {
        var parsed = WeddingGuestMetadataMapper.TryParseRelationship(value, out var relationship);

        Assert.True(parsed);
        Assert.Equal(expected, relationship);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TryParseRelationship_WhenBlank_ReturnsTrueWithNull(string? value)
    {
        var parsed = WeddingGuestMetadataMapper.TryParseRelationship(value, out var relationship);

        Assert.True(parsed);
        Assert.Null(relationship);
    }

    [Fact]
    public void TryParseRelationship_WhenUnknown_ReturnsFalse()
    {
        var parsed = WeddingGuestMetadataMapper.TryParseRelationship("Nemesis", out var relationship);

        Assert.False(parsed);
        Assert.Null(relationship);
    }

    [Theory]
    [InlineData("Bride", GuestSide.Bride)]
    [InlineData("groom", GuestSide.Groom)]
    public void TryParseSide_WhenKnown_ReturnsValue(string value, GuestSide expected)
    {
        var parsed = WeddingGuestMetadataMapper.TryParseSide(value, out var side);

        Assert.True(parsed);
        Assert.Equal(expected, side);
    }

    [Fact]
    public void TryParseSide_WhenUnknown_ReturnsFalse()
    {
        var parsed = WeddingGuestMetadataMapper.TryParseSide("Neither", out var side);

        Assert.False(parsed);
        Assert.Null(side);
    }

    [Fact]
    public void Serialize_WhenNothingProvided_ReturnsNull()
    {
        var metadata = WeddingGuestMetadata.Create(null, null, 0, null);

        Assert.Null(WeddingGuestMetadataMapper.Serialize(metadata));
    }

    [Fact]
    public void SerializeThenDeserialize_RoundTrips()
    {
        var metadata = WeddingGuestMetadata.Create(Relationship.Family, GuestSide.Bride, 2, "Pescatarian");

        var json = WeddingGuestMetadataMapper.Serialize(metadata);
        Assert.NotNull(json);
        Assert.Contains("\"relationship\":\"Family\"", json);

        var restored = WeddingGuestMetadataMapper.Deserialize(json);
        Assert.Equal(Relationship.Family, restored.Relationship);
        Assert.Equal(GuestSide.Bride, restored.Side);
        Assert.Equal(2, restored.PlusOnes);
        Assert.Equal("Pescatarian", restored.DietaryNotes);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-json")]
    public void Deserialize_WhenMissingOrInvalid_ReturnsEmptyMetadata(string? json)
    {
        var restored = WeddingGuestMetadataMapper.Deserialize(json);

        Assert.Null(restored.Relationship);
        Assert.Null(restored.Side);
        Assert.Equal(0, restored.PlusOnes);
        Assert.Null(restored.DietaryNotes);
    }
}
