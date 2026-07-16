using System.Text.Json;
using FluentValidation;
using GuestManagementService.Application.Guests.Wedding;
using GuestManagementService.Contracts.Guests.Wedding;
using GuestManagementService.Domain.Guests.Wedding;

namespace GuestManagementService.UnitTests.Guests.Wedding;

public sealed class WeddingGuestMetadataMapperTests
{
    private readonly WeddingGuestMetadataMapper mapper = new();

    [Fact]
    public void EventType_IsWedding()
    {
        Assert.Equal("wedding", mapper.EventType);
    }

    [Fact]
    public void InstanceSerialize_WhenEventMetadataIsNull_ReturnsNull()
    {
        Assert.Null(mapper.Serialize(null));
    }

    [Fact]
    public void InstanceSerialize_WhenFieldsAreValid_ReturnsJson()
    {
        var element = ParseElement(new
        {
            relationship = "Family",
            side = "Bride",
            plusOnes = 2,
            dietaryNotes = "Pescatarian"
        });

        var json = mapper.Serialize(element);

        Assert.NotNull(json);
        Assert.Contains("\"relationship\":\"Family\"", json);
    }

    [Fact]
    public void InstanceSerialize_WhenRelationshipIsInvalid_ThrowsValidationException()
    {
        var element = ParseElement(new { relationship = "Nemesis" });

        var exception = Assert.Throws<ValidationException>(() => mapper.Serialize(element));
        Assert.Contains(exception.Errors, error => error.PropertyName == "EventMetadata.Relationship");
    }

    [Fact]
    public void InstanceSerialize_WhenPlusOnesOutOfRange_ThrowsValidationException()
    {
        var element = ParseElement(new { plusOnes = 21 });

        var exception = Assert.Throws<ValidationException>(() => mapper.Serialize(element));
        Assert.Contains(exception.Errors, error => error.PropertyName == "EventMetadata.PlusOnes");
    }

    [Fact]
    public void InstanceSerialize_WhenDietaryNotesTooLong_ThrowsValidationException()
    {
        var element = ParseElement(new { dietaryNotes = new string('a', 501) });

        var exception = Assert.Throws<ValidationException>(() => mapper.Serialize(element));
        Assert.Contains(exception.Errors, error => error.PropertyName == "EventMetadata.DietaryNotes");
    }

    [Fact]
    public void ToContract_WhenStoredMetadataIsNull_ReturnsNull()
    {
        Assert.Null(mapper.ToContract(null));
    }

    [Fact]
    public void ToContract_WhenStoredMetadataPresent_ReturnsWeddingResponse()
    {
        var json = WeddingGuestMetadataMapper.Serialize(
            WeddingGuestMetadata.Create(Relationship.Family, GuestSide.Bride, 2, "Pescatarian"));

        var contract = Assert.IsType<WeddingGuestMetadataResponse>(mapper.ToContract(json));

        Assert.Equal("Family", contract.Relationship);
        Assert.Equal("Bride", contract.Side);
        Assert.Equal(2, contract.PlusOnes);
        Assert.Equal("Pescatarian", contract.DietaryNotes);
    }

    private static JsonElement ParseElement(object value)
    {
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(value));
        return document.RootElement.Clone();
    }

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
