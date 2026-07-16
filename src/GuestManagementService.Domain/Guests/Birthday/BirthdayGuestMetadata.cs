namespace GuestManagementService.Domain.Guests.Birthday;

/// <summary>
/// Event-type-specific attributes for a guest of a <c>birthday</c> event. Persisted inside the
/// guest's opaque <see cref="Guest.Metadata"/> JSON column. New event types get their own
/// metadata type under their own folder rather than new columns.
/// </summary>
public sealed record BirthdayGuestMetadata
{
    private BirthdayGuestMetadata(int plusOnes, string? dietaryNotes)
    {
        PlusOnes = plusOnes;
        DietaryNotes = dietaryNotes;
    }

    public int PlusOnes { get; }

    public string? DietaryNotes { get; }

    public static BirthdayGuestMetadata Create(int plusOnes, string? dietaryNotes)
    {
        if (plusOnes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(plusOnes), "Plus-ones must not be negative.");
        }

        var cleanDietaryNotes = string.IsNullOrWhiteSpace(dietaryNotes) ? null : dietaryNotes.Trim();

        return new BirthdayGuestMetadata(plusOnes, cleanDietaryNotes);
    }
}
