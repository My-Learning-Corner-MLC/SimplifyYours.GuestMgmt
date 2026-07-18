namespace GuestManagementService.Domain.Guests.Wedding;

/// <summary>
/// Event-type-specific attributes for a guest of a <c>wedding</c> event. Persisted inside the
/// guest's opaque <see cref="Guest.Metadata"/> JSON column. New event types get their own
/// metadata type under their own folder rather than new columns.
/// </summary>
public sealed record WeddingGuestMetadata
{
    public const int MaxTags = 10;
    public const int MaxTagLength = 32;

    private WeddingGuestMetadata(
        Relationship? relationship,
        GuestSide? side,
        int plusOnes,
        string? dietaryNotes,
        IReadOnlyList<string> tags)
    {
        Relationship = relationship;
        Side = side;
        PlusOnes = plusOnes;
        DietaryNotes = dietaryNotes;
        Tags = tags;
    }

    public Relationship? Relationship { get; }

    public GuestSide? Side { get; }

    public int PlusOnes { get; }

    public string? DietaryNotes { get; }

    // Table tags — free-text labels a guest can carry to speed up seating (e.g. "College
    // friends"). Wedding-specific, like the rest of this metadata.
    public IReadOnlyList<string> Tags { get; }

    public static WeddingGuestMetadata Create(
        Relationship? relationship,
        GuestSide? side,
        int plusOnes,
        string? dietaryNotes,
        IReadOnlyList<string>? tags = null)
    {
        if (plusOnes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(plusOnes), "Plus-ones must not be negative.");
        }

        var cleanDietaryNotes = string.IsNullOrWhiteSpace(dietaryNotes) ? null : dietaryNotes.Trim();
        var cleanTags = NormalizeTags(tags);

        return new WeddingGuestMetadata(relationship, side, plusOnes, cleanDietaryNotes, cleanTags);
    }

    private static IReadOnlyList<string> NormalizeTags(IReadOnlyList<string>? tags)
    {
        if (tags is null || tags.Count == 0)
        {
            return Array.Empty<string>();
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<string>();
        foreach (var tag in tags)
        {
            var trimmed = tag?.Trim();
            if (string.IsNullOrEmpty(trimmed))
            {
                continue;
            }

            if (trimmed.Length > MaxTagLength)
            {
                throw new ArgumentException($"Tags must be {MaxTagLength} characters or fewer.", nameof(tags));
            }

            if (seen.Add(trimmed))
            {
                result.Add(trimmed);
            }
        }

        if (result.Count > MaxTags)
        {
            throw new ArgumentException($"A guest may have at most {MaxTags} tags.", nameof(tags));
        }

        return result;
    }
}
