using GuestManagementService.Application.Invitations.SubmitRsvp;

namespace GuestManagementService.UnitTests.Invitations;

/// <summary>
/// The shape rules for the one write in this system reachable without authenticating.
/// </summary>
/// <remarks>
/// These exist because the handler tests construct the handler directly and never run the MediatR
/// <c>ValidationBehavior</c>, so nothing else in the suite executes a single rule in this class.
/// Every rule below is the only thing standing between an anonymous caller and the domain.
/// </remarks>
public sealed class SubmitRsvpCommandValidatorTests
{
    private static readonly SubmitRsvpCommandValidator Validator = new();

    private static SubmitRsvpCommand Command(
        string? status = "Accepted",
        int? plusOnes = null,
        string? notes = null) =>
        new("tok-abc123", status, plusOnes, notes);

    private static string[] ErrorsFor(SubmitRsvpCommand command, string property) =>
        Validator.Validate(command).Errors
            .Where(error => error.PropertyName == property)
            .Select(error => error.ErrorMessage)
            .ToArray();

    // ---------- status ----------

    [Theory]
    [InlineData("Accepted")]
    [InlineData("Declined")]
    [InlineData("Maybe")]
    [InlineData("accepted")]
    public void Accepts_TheThreeAnswers_CaseInsensitively(string status)
    {
        Assert.True(Validator.Validate(Command(status)).IsValid);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Rejects_AMissingAnswer(string? status)
    {
        Assert.Contains(
            "Choose whether you are attending.",
            ErrorsFor(Command(status), nameof(SubmitRsvpCommand.RsvpStatus)));
    }

    [Fact]
    public void Rejects_NoResponse_WhichIsAStateNotAnAnswer()
    {
        // NoResponse is the initial state of every guest. Accepting it as a submission would let a
        // caller silently reset an answer they had already given.
        Assert.Contains(
            "Choose one of: Accepted, Declined, Maybe.",
            ErrorsFor(Command("NoResponse"), nameof(SubmitRsvpCommand.RsvpStatus)));
    }

    [Fact]
    public void Rejects_AnUnknownStatus()
    {
        Assert.NotEmpty(ErrorsFor(Command("Perhaps"), nameof(SubmitRsvpCommand.RsvpStatus)));
    }

    // ---------- AC 43: only Attending carries a count ----------

    [Theory]
    [InlineData("Declined")]
    [InlineData("Maybe")]
    public void Rejects_AGuestCount_OnAnythingButAttending(string status)
    {
        // AC 43. Deleting the .When(!IsAccepted) condition would make this pass silently, which is
        // exactly the regression this test exists to catch.
        Assert.Contains(
            "Guests can only be added when you are attending.",
            ErrorsFor(Command(status, plusOnes: 2), nameof(SubmitRsvpCommand.PlusOnesConfirmed)));
    }

    [Theory]
    [InlineData("Declined")]
    [InlineData("Maybe")]
    public void Allows_AZeroOrAbsentCount_OnAnythingButAttending(string status)
    {
        Assert.True(Validator.Validate(Command(status, plusOnes: 0)).IsValid);
        Assert.True(Validator.Validate(Command(status)).IsValid);
    }

    [Fact]
    public void Allows_AGuestCount_WhenAttending()
    {
        Assert.True(Validator.Validate(Command("Accepted", plusOnes: 3)).IsValid);
    }

    [Fact]
    public void Rejects_ANegativeGuestCount()
    {
        Assert.Contains(
            "Number of guests cannot be negative.",
            ErrorsFor(Command("Accepted", plusOnes: -1), nameof(SubmitRsvpCommand.PlusOnesConfirmed)));
    }

    // ---------- AC 24: notes cap ----------

    [Fact]
    public void Allows_NotesAtExactlyTheCap()
    {
        var atCap = new string('x', SubmitRsvpCommandValidator.DietaryNotesMaxLength);

        Assert.True(Validator.Validate(Command(notes: atCap)).IsValid);
    }

    [Fact]
    public void Rejects_NotesOneCharacterOverTheCap()
    {
        // AC 24. The boundary is the whole point: an off-by-one here is a 501-character string
        // reaching a column sized for 500.
        var overCap = new string('x', SubmitRsvpCommandValidator.DietaryNotesMaxLength + 1);

        Assert.Contains(
            "Notes must be 500 characters or fewer.",
            ErrorsFor(Command(notes: overCap), nameof(SubmitRsvpCommand.DietaryNotes)));
    }

    [Fact]
    public void Allows_AbsentNotes()
    {
        Assert.True(Validator.Validate(Command(notes: null)).IsValid);
    }
}
