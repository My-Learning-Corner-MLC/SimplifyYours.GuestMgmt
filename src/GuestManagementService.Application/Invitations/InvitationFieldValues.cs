using System.Text.Json;
using System.Text.Json.Nodes;

namespace GuestManagementService.Application.Invitations;

/// <summary>
/// Reads and writes the JSON blob stored in <c>event_invitation_settings.field_values</c>.
/// </summary>
public static class InvitationFieldValues
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public static IReadOnlyDictionary<string, string?> Parse(string? json)
    {
        var values = new Dictionary<string, string?>(StringComparer.Ordinal);

        if (string.IsNullOrWhiteSpace(json))
        {
            return values;
        }

        JsonObject? parsed;

        try
        {
            parsed = JsonNode.Parse(json) as JsonObject;
        }
        catch (JsonException)
        {
            // Unreadable stored content must not take down the organiser's form or a guest's page.
            // Treating it as empty degrades gracefully; the organiser can re-save.
            return values;
        }

        if (parsed is null)
        {
            return values;
        }

        foreach (var (key, node) in parsed)
        {
            values[key] = node?.GetValue<string?>();
        }

        return values;
    }

    /// <summary>
    /// Serialises only the fields the event type actually declares, so an unknown key cannot be
    /// smuggled into storage even if validation is bypassed one day.
    /// </summary>
    public static string Serialize(IReadOnlyDictionary<string, string?> values, string eventType)
    {
        var allowed = InvitationFieldSchema.AllFor(eventType);
        var filtered = new Dictionary<string, string?>(StringComparer.Ordinal);

        foreach (var field in allowed)
        {
            if (values.TryGetValue(field, out var value))
            {
                var trimmed = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
                filtered[field] = trimmed;
            }
        }

        return JsonSerializer.Serialize(filtered, SerializerOptions);
    }
}
