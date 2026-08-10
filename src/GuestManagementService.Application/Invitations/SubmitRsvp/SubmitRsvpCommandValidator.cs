using FluentValidation;
using GuestManagementService.Domain.Guests;

namespace GuestManagementService.Application.Invitations.SubmitRsvp;

/// <summary>
/// Shape validation for an anonymous, public write.
/// </summary>
/// <remarks>
/// Bounds that depend on the guest's own record — chiefly that plus-ones cannot exceed the
/// organiser's allowance — are enforced in the handler, which is the only place that has loaded
/// the guest. This validator covers everything checkable from the request alone.
/// </remarks>
public sealed class SubmitRsvpCommandValidator : AbstractValidator<SubmitRsvpCommand>
{
    public const int DietaryNotesMaxLength = 500;

    public SubmitRsvpCommandValidator()
    {
        RuleFor(command => command.RsvpStatus)
            .NotEmpty()
            .WithMessage("Choose whether you are attending.")
            .Must(BeARespondableStatus)
            .WithMessage("Choose one of: Accepted, Declined, Maybe.");

        RuleFor(command => command.PlusOnesConfirmed)
            .GreaterThanOrEqualTo(0)
            .When(command => command.PlusOnesConfirmed.HasValue)
            .WithMessage("Number of guests cannot be negative.");

        RuleFor(command => command.DietaryNotes)
            .MaximumLength(DietaryNotesMaxLength)
            .WithMessage($"Notes must be {DietaryNotesMaxLength} characters or fewer.");

        // Only "Attending" plans for extra people. Maybe and Declined show no guest stepper at all
        // in the design, so a count arriving with either is a client that has drifted from it.
        RuleFor(command => command.PlusOnesConfirmed)
            .Must(count => count is null or 0)
            .When(command => !IsAccepted(command.RsvpStatus))
            .WithMessage("Guests can only be added when you are attending.");
    }

    private static bool BeARespondableStatus(string? value)
    {
        return TryParse(value, out var status) && status != Domain.Guests.RsvpStatus.NoResponse;
    }

    private static bool IsAccepted(string? value)
    {
        return TryParse(value, out var status) && status == Domain.Guests.RsvpStatus.Accepted;
    }

    /// <summary>Parses the contract-facing value, case-insensitively.</summary>
    public static bool TryParse(string? value, out RsvpStatus status)
    {
        return Enum.TryParse(value, ignoreCase: true, out status)
               && Enum.IsDefined(status);
    }
}
