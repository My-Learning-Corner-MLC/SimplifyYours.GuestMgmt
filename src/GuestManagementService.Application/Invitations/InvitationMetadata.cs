using System.Text.Json;
using System.Text.Json.Nodes;

namespace GuestManagementService.Application.Invitations;

/// <summary>
/// Reads and writes the handful of guest-metadata fields the invitation flow cares about.
/// </summary>
/// <remarks>
/// Guest metadata is an opaque jsonb blob whose shape varies by event type
/// (<c>IGuestMetadataMapper</c>). The invitation flow needs only <c>plusOnes</c>,
/// <c>plusOnesConfirmed</c> and <c>dietaryNotes</c>, which are named the same across every event
/// type, so it reads those keys directly rather than forcing every mapper to grow an
/// invitation-shaped contract.
/// <para>
/// <c>plusOnes</c> is the organiser's allowance and is never written here.
/// <c>plusOnesConfirmed</c> and <c>dietaryNotes</c> are the guest's own answers.
/// </para>
/// </remarks>
public static class InvitationMetadata
{
    public const string PlusOnesAllowedKey = "plusOnes";
    public const string PlusOnesConfirmedKey = "plusOnesConfirmed";
    public const string DietaryNotesKey = "dietaryNotes";

    public static int ReadPlusOnesAllowed(string? metadataJson)
    {
        var node = Parse(metadataJson);

        return node?[PlusOnesAllowedKey]?.GetValue<int?>() ?? 0;
    }

    public static int? ReadPlusOnesConfirmed(string? metadataJson)
    {
        return Parse(metadataJson)?[PlusOnesConfirmedKey]?.GetValue<int?>();
    }

    public static string? ReadDietaryNotes(string? metadataJson)
    {
        var value = Parse(metadataJson)?[DietaryNotesKey]?.GetValue<string?>();

        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    /// <summary>
    /// Returns the metadata with the guest's answers applied, leaving every other field — including
    /// the organiser's <c>plusOnes</c> allowance — exactly as it was.
    /// </summary>
    public static string WriteGuestAnswers(string? metadataJson, int plusOnesConfirmed, string? dietaryNotes)
    {
        var node = Parse(metadataJson) ?? new JsonObject();

        node[PlusOnesConfirmedKey] = plusOnesConfirmed;
        node[DietaryNotesKey] = string.IsNullOrWhiteSpace(dietaryNotes) ? null : dietaryNotes.Trim();

        return node.ToJsonString();
    }

    private static JsonObject? Parse(string? metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
        {
            return null;
        }

        try
        {
            return JsonNode.Parse(metadataJson) as JsonObject;
        }
        catch (JsonException)
        {
            // Unparseable metadata must not take down a guest's invitation page. Treating it as
            // absent degrades one field; throwing would return 500 for the whole invitation.
            return null;
        }
    }
}
