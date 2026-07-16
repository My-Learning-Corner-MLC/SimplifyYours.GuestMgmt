using System.Text.Json;
using System.Text.Json.Serialization;
using FluentValidation;
using FluentValidation.Results;
using GuestManagementService.Contracts.Guests.Birthday;
using GuestManagementService.Domain.Guests.Birthday;

namespace GuestManagementService.Application.Guests.Birthday;

/// <summary>
/// Parses request values into a <see cref="BirthdayGuestMetadata"/> and (de)serializes it to the
/// JSON stored in the guest's opaque metadata column. Birthday-specific — narrower than wedding:
/// just plus-ones and dietary notes, no relationship/side.
/// </summary>
public sealed class BirthdayGuestMetadataMapper(IValidator<BirthdayGuestMetadataRequest> validator)
    : IGuestMetadataMapper
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public string EventType => "birthday";

    public string? Serialize(JsonElement? eventMetadata)
    {
        if (eventMetadata is null || eventMetadata.Value.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
        {
            return null;
        }

        BirthdayGuestMetadataRequest? request;
        try
        {
            request = eventMetadata.Value.Deserialize<BirthdayGuestMetadataRequest>(SerializerOptions);
        }
        catch (JsonException)
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(
                    "EventMetadata",
                    "Event metadata must be a valid object for a birthday guest."),
            });
        }

        request ??= new BirthdayGuestMetadataRequest(null, null);

        var validationResult = validator.Validate(request);
        if (!validationResult.IsValid)
        {
            var failures = validationResult.Errors
                .Select(error => new ValidationFailure($"EventMetadata.{error.PropertyName}", error.ErrorMessage))
                .ToList();
            throw new ValidationException(failures);
        }

        var metadata = BirthdayGuestMetadata.Create(request.PlusOnes ?? 0, request.DietaryNotes);
        return Serialize(metadata);
    }

    public object? ToContract(string? storedMetadata)
    {
        var metadata = Deserialize(storedMetadata);

        if (metadata is { PlusOnes: 0, DietaryNotes: null })
        {
            return null;
        }

        return new BirthdayGuestMetadataResponse(metadata.PlusOnes, metadata.DietaryNotes);
    }

    /// <summary>Serializes the birthday metadata to JSON, or <c>null</c> when nothing was provided.</summary>
    public static string? Serialize(BirthdayGuestMetadata metadata)
    {
        var document = new MetadataDocument(metadata.PlusOnes, metadata.DietaryNotes);

        if (document is { PlusOnes: 0, DietaryNotes: null })
        {
            return null;
        }

        return JsonSerializer.Serialize(document, SerializerOptions);
    }

    /// <summary>Reads stored JSON back into a <see cref="BirthdayGuestMetadata"/>; empty when absent/unreadable.</summary>
    public static BirthdayGuestMetadata Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return BirthdayGuestMetadata.Create(0, null);
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
            return BirthdayGuestMetadata.Create(0, null);
        }

        var plusOnes = document.PlusOnes < 0 ? 0 : document.PlusOnes;

        return BirthdayGuestMetadata.Create(plusOnes, document.DietaryNotes);
    }

    private sealed record MetadataDocument(int PlusOnes, string? DietaryNotes);
}
