using System.Text.Json;

namespace GuestManagementService.Contracts.Guests;

public sealed record GuestInfoRequest(
    string? FirstName,
    string? LastName,
    string? PhoneNumber,
    string? EmailAddress,
    string? Gender,
    // Shape depends on the event's type (e.g. wedding: relationship, side, plusOnes,
    // dietaryNotes) — see IGuestMetadataMapper. Null/omitted for event types with no metadata.
    JsonElement? EventMetadata = null,
    // Free-text seating labels (e.g. "College friends"). Applies to every event type. Optional.
    IReadOnlyList<string>? Tags = null);
