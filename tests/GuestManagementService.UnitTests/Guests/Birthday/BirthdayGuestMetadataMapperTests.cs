using System.Text.Json;
using FluentValidation;
using GuestManagementService.Application.Guests.Birthday;
using GuestManagementService.Contracts.Guests.Birthday;
using GuestManagementService.Domain.Guests.Birthday;

namespace GuestManagementService.UnitTests.Guests.Birthday;

public sealed class BirthdayGuestMetadataMapperTests
{
    private readonly BirthdayGuestMetadataMapper mapper = new();

    [Fact]
    public void EventType_IsBirthday()
    {
        Assert.Equal("birthday", mapper.EventType);
    }

    [Fact]
    public void Serialize_WhenEventMetadataIsNull_ReturnsNull()
    {
        Assert.Null(mapper.Serialize(null));
    }

    [Fact]
    public void Serialize_WhenFieldsAreValid_ReturnsJson()
    {
        var element = ParseElement(new { plusOnes = 2, dietaryNotes = "Vegan" });

        var json = mapper.Serialize(element);

        Assert.NotNull(json);
        Assert.Contains("\"plusOnes\":2", json);
        Assert.Contains("\"dietaryNotes\":\"Vegan\"", json);
    }

    [Fact]
    public void Serialize_WhenPlusOnesOutOfRange_ThrowsValidationException()
    {
        var element = ParseElement(new { plusOnes = 21 });

        var exception = Assert.Throws<ValidationException>(() => mapper.Serialize(element));
        Assert.Contains(exception.Errors, error => error.PropertyName == "EventMetadata.PlusOnes");
    }

    [Fact]
    public void Serialize_WhenDietaryNotesTooLong_ThrowsValidationException()
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
    public void ToContract_WhenStoredMetadataPresent_ReturnsBirthdayResponse()
    {
        var json = BirthdayGuestMetadataMapper.Serialize(BirthdayGuestMetadata.Create(3, "Nut allergy"));

        var contract = Assert.IsType<BirthdayGuestMetadataResponse>(mapper.ToContract(json));

        Assert.Equal(3, contract.PlusOnes);
        Assert.Equal("Nut allergy", contract.DietaryNotes);
    }

    [Fact]
    public void SerializeThenDeserialize_RoundTrips()
    {
        var metadata = BirthdayGuestMetadata.Create(3, "Nut allergy");

        var json = BirthdayGuestMetadataMapper.Serialize(metadata);
        Assert.NotNull(json);

        var restored = BirthdayGuestMetadataMapper.Deserialize(json);
        Assert.Equal(3, restored.PlusOnes);
        Assert.Equal("Nut allergy", restored.DietaryNotes);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-json")]
    public void Deserialize_WhenMissingOrInvalid_ReturnsEmptyMetadata(string? json)
    {
        var restored = BirthdayGuestMetadataMapper.Deserialize(json);

        Assert.Equal(0, restored.PlusOnes);
        Assert.Null(restored.DietaryNotes);
    }

    private static JsonElement ParseElement(object value)
    {
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(value));
        return document.RootElement.Clone();
    }
}
