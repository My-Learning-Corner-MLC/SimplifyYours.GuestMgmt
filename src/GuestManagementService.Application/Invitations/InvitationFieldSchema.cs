using FluentValidation;
using FluentValidation.Results;

namespace GuestManagementService.Application.Invitations;

/// <summary>
/// Which merge fields an organiser fills in for a given event type, and how long each may be.
/// </summary>
/// <remarks>
/// One definition serves three callers: the pre-fill defaults, the save validator, and the
/// renderer's allowlist. Keeping them from drifting apart matters more than the few lines saved by
/// inlining any one of them — a field the form collects but the renderer ignores is invisible until
/// a guest sees a blank invitation.
/// <para>
/// <c>guestName</c> is deliberately absent: it is never typed. It comes from the guest whose link
/// is being opened.
/// </para>
/// </remarks>
public static class InvitationFieldSchema
{
    public const string Wedding = "wedding";
    public const string Birthday = "birthday";

    public const string BrideName = "brideName";
    public const string GroomName = "groomName";
    public const string EventName = "eventName";
    public const string EventDate = "eventDate";
    public const string EventTime = "eventTime";
    public const string VenueName = "venueName";
    public const string VenueAddress = "venueAddress";
    public const string VenueNotes = "venueNotes";

    /// <summary>Caps mirroring EventService.Domain.Events.EventLocation, so nothing overflows.</summary>
    private static readonly IReadOnlyDictionary<string, int> MaxLengths =
        new Dictionary<string, int>(StringComparer.Ordinal)
        {
            [BrideName] = 200,
            [GroomName] = 200,
            [EventName] = 200,
            [EventDate] = 100,
            [EventTime] = 100,
            [VenueName] = 200,
            [VenueAddress] = 500,
            [VenueNotes] = 2000,
        };

    private static readonly IReadOnlyList<string> WeddingRequired =
        [BrideName, GroomName, EventDate, EventTime, VenueName, VenueAddress];

    private static readonly IReadOnlyList<string> BirthdayRequired =
        [EventName, EventDate, EventTime, VenueName, VenueAddress];

    /// <summary>
    /// Notes are the one optional field — "parking at the rear" is a nicety, and blocking a save on
    /// it would be a false gate.
    /// </summary>
    private static readonly IReadOnlyList<string> Optional = [VenueNotes];

    public static bool IsSupported(string? eventType) =>
        Matches(eventType, Wedding) || Matches(eventType, Birthday);

    public static IReadOnlyList<string> RequiredFor(string eventType)
    {
        EnsureSupported(eventType);

        return Matches(eventType, Wedding) ? WeddingRequired : BirthdayRequired;
    }

    /// <summary>Every field the form collects for this event type, required and optional.</summary>
    public static IReadOnlyList<string> AllFor(string eventType) =>
        [.. RequiredFor(eventType), .. Optional];

    public static int MaxLengthOf(string field) =>
        MaxLengths.TryGetValue(field, out var max) ? max : 200;

    /// <summary>
    /// Throws when the event type has no invitation field set. Mirrors how
    /// <c>GuestMetadataMapperFactory</c> refuses unsupported types, rather than silently accepting
    /// an event nobody can compose an invitation for.
    /// </summary>
    public static void EnsureSupported(string? eventType)
    {
        if (IsSupported(eventType))
        {
            return;
        }

        throw new ValidationException(
        [
            new ValidationFailure(
                "EventType",
                $"Invitations are not supported for '{eventType}' events yet. Supported types: {Birthday}, {Wedding}."),
        ]);
    }

    private static bool Matches(string? eventType, string expected) =>
        string.Equals(eventType, expected, StringComparison.OrdinalIgnoreCase);
}
