namespace GuestManagementService.Domain.Guests.Wedding;

/// <summary>
/// Event-type-specific attributes for a guest of a <c>wedding</c> event. Persisted inside the
/// guest's opaque <see cref="Guest.Metadata"/> JSON column. New event types get their own
/// metadata type under their own folder rather than new columns.
/// </summary>
public sealed record WeddingGuestMetadata
{
    private WeddingGuestMetadata(
        Relationship? relationship,
        GuestSide? side,
        int plusOnes,
        string? dietaryNotes)
    {
        Relationship = relationship;
        Side = side;
        PlusOnes = plusOnes;
        DietaryNotes = dietaryNotes;
    }

    public Relationship? Relationship { get; }

    public GuestSide? Side { get; }

    public int PlusOnes { get; }

    public string? DietaryNotes { get; }

    public static WeddingGuestMetadata Create(
        Relationship? relationship,
        GuestSide? side,
        int plusOnes,
        string? dietaryNotes)
    {
        if (plusOnes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(plusOnes), "Plus-ones must not be negative.");
        }

        var cleanDietaryNotes = string.IsNullOrWhiteSpace(dietaryNotes) ? null : dietaryNotes.Trim();

        return new WeddingGuestMetadata(relationship, side, plusOnes, cleanDietaryNotes);
    }
}
