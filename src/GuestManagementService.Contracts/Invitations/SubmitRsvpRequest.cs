namespace GuestManagementService.Contracts.Invitations;

public sealed record SubmitRsvpRequest(
    string? RsvpStatus,
    int? PlusOnesConfirmed,
    string? DietaryNotes);
