namespace GuestManagementService.Domain.Guests;

/// <summary>
/// How far a guest's invitation has got toward being delivered. Tracked separately from
/// <see cref="RsvpStatus"/> because the two are genuinely independent: an organiser can copy a
/// guest's link by hand and receive a response without any invitation ever being sent.
/// </summary>
public enum DeliveryStatus
{
    NotSent = 0,
    Queued = 1,
    Sent = 2,
    Failed = 3,
}
