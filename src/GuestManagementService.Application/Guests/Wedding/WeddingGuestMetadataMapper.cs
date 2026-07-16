using System.Text.Json;
using System.Text.Json.Serialization;
using FluentValidation;
using FluentValidation.Results;
using GuestManagementService.Contracts.Guests.Wedding;
using GuestManagementService.Domain.Guests.Wedding;

namespace GuestManagementService.Application.Guests.Wedding;

/// <summary>
/// Parses request values into a <see cref="WeddingGuestMetadata"/> and (de)serializes it to the
/// JSON stored in the guest's opaque metadata column. Wedding-specific — other event types get
/// their own mapper under their own folder and are registered alongside this one in DI; see
/// <see cref="IGuestMetadataMapperFactory"/>.
/// </summary>
public sealed class WeddingGuestMetadataMapper(IValidator<WeddingGuestMetadataRequest> validator)
    : IGuestMetadataMapper
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public string EventType => "wedding";

    public string? Serialize(JsonElement? eventMetadata)
    {
        if (eventMetadata is null || eventMetadata.Value.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
        {
            return null;
        }

        WeddingGuestMetadataRequest? request;
        try
        {
            request = eventMetadata.Value.Deserialize<WeddingGuestMetadataRequest>(SerializerOptions);
        }
        catch (JsonException)
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(
                    "EventMetadata",
                    "Event metadata must be a valid object for a wedding guest."),
            });
        }

        request ??= new WeddingGuestMetadataRequest(null, null, null, null);

        var validationResult = validator.Validate(request);
        if (!validationResult.IsValid)
        {
            var failures = validationResult.Errors
                .Select(error => new ValidationFailure($"EventMetadata.{error.PropertyName}", error.ErrorMessage))
                .ToList();
            throw new ValidationException(failures);
        }

        TryParseRelationship(request.Relationship, out var relationship);
        TryParseSide(request.Side, out var side);
        var metadata = WeddingGuestMetadata.Create(relationship, side, request.PlusOnes ?? 0, request.DietaryNotes);
        return Serialize(metadata);
    }

    public object? ToContract(string? storedMetadata)
    {
        var metadata = Deserialize(storedMetadata);

        if (metadata is { Relationship: null, Side: null, PlusOnes: 0, DietaryNotes: null })
        {
            return null;
        }

        return new WeddingGuestMetadataResponse(
            ToContractValue(metadata.Relationship),
            ToContractValue(metadata.Side),
            metadata.PlusOnes,
            metadata.DietaryNotes);
    }

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
