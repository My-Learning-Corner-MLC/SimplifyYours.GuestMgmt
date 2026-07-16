using System.Text.Json;

namespace GuestManagementService.Application.Guests;

/// <summary>
/// Parses/validates a guest's opaque <c>eventMetadata</c> request payload into the JSON stored on
/// <see cref="Domain.Guests.Guest.Metadata"/>, and maps it back to a contract-facing shape for
/// responses. One implementation per event type, registered in DI and resolved by
/// <see cref="IGuestMetadataMapperFactory"/> — new event types add a mapper under their own
/// folder, no changes required here or in the factory.
/// </summary>
public interface IGuestMetadataMapper
{
    /// <summary>The event type this mapper handles (e.g. "wedding"), matched case-insensitively.</summary>
    string EventType { get; }

    /// <summary>
    /// Validates and serializes the request's <paramref name="eventMetadata"/> to the JSON stored
    /// on the guest, or <c>null</c> when nothing was provided. Throws
    /// <see cref="FluentValidation.ValidationException"/> when a field is invalid.
    /// </summary>
    string? Serialize(JsonElement? eventMetadata);

    /// <summary>Deserializes the guest's stored metadata JSON into the contract-facing shape.</summary>
    object? ToContract(string? storedMetadata);
}
