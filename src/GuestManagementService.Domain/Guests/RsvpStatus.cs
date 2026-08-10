namespace GuestManagementService.Domain.Guests;

/// <summary>
/// A guest's answer. <see cref="Maybe"/> is stored and displayed but never counted toward
/// headcount — an organiser ordering catering should not be led into paying for maybes.
/// </summary>
public enum RsvpStatus
{
    NoResponse = 0,
    Accepted = 1,
    Declined = 2,
    Maybe = 3,
}
