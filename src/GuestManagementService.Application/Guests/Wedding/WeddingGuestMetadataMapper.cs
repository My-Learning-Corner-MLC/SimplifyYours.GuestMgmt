using System.Text.Json;
using System.Text.Json.Serialization;
using GuestManagementService.Domain.Guests.Wedding;

namespace GuestManagementService.Application.Guests.Wedding;

/// <summary>
/// Parses request values into a <see cref="WeddingGuestMetadata"/> and (de)serializes it to the
/// JSON stored in the guest's opaque metadata column. Wedding-specific — other event types get
/// their own mapper under their own folder.
/// </summary>
public static class WeddingGuestMetadataMapper
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static bool TryParseRelationship(string? value, out Relationship? relationship)
    {
        relationship = null;

        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        if (Enum.TryParse<Relationship>(value.Trim(), ignoreCase: true, out var parsed)
            && Enum.IsDefined(parsed))
        {
            relationship = parsed;
            return true;
        }

        return false;
    }

    public static bool TryParseSide(string? value, out GuestSide? side)
    {
        side = null;

        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        if (Enum.TryParse<GuestSide>(value.Trim(), ignoreCase: true, out var parsed)
            && Enum.IsDefined(parsed))
        {
            side = parsed;
            return true;
        }

        return false;
    }

    /// <summary>Serializes the wedding metadata to JSON, or <c>null</c> when nothing was provided.</summary>
    public static string? Serialize(WeddingGuestMetadata metadata)
    {
        var document = new MetadataDocument(
            metadata.Relationship?.ToString(),
            metadata.Side?.ToString(),
            metadata.PlusOnes,
            metadata.DietaryNotes);

        if (document is { Relationship: null, Side: null, PlusOnes: 0, DietaryNotes: null })
        {
            return null;
        }

        return JsonSerializer.Serialize(document, SerializerOptions);
    }

    /// <summary>Reads stored JSON back into a <see cref="WeddingGuestMetadata"/>; empty when absent/unreadable.</summary>
    public static WeddingGuestMetadata Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return WeddingGuestMetadata.Create(null, null, 0, null);
        }

        MetadataDocument? document;
        try
        {
            document = JsonSerializer.Deserialize<MetadataDocument>(json, SerializerOptions);
        }
        catch (JsonException)
        {
            document = null;
        }

        if (document is null)
        {
            return WeddingGuestMetadata.Create(null, null, 0, null);
        }

        TryParseRelationship(document.Relationship, out var relationship);
        TryParseSide(document.Side, out var side);
        var plusOnes = document.PlusOnes < 0 ? 0 : document.PlusOnes;

        return WeddingGuestMetadata.Create(relationship, side, plusOnes, document.DietaryNotes);
    }

    public static string? ToContractValue(Relationship? relationship)
    {
        return relationship?.ToString();
    }

    public static string? ToContractValue(GuestSide? side)
    {
        return side?.ToString();
    }

    private sealed record MetadataDocument(
        string? Relationship,
        string? Side,
        int PlusOnes,
        string? DietaryNotes);
}
