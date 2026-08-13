namespace GuestManagementService.Api.Security;

public static class Permissions
{
    public const string ClaimType = "permissions";

    public const string GuestsAdd = "guests.add";

    public const string GuestsView = "guests.view";

    /// <summary>
    /// Declared by event-service and reused here: composing an event's invitation is editing that
    /// event's presentation, so whoever may update the event may compose its invitation. No new
    /// permission is introduced, which keeps the identity-service catalog untouched.
    /// </summary>
    /// <remarks>
    /// Permissions travel in the JWT, so checking this needs no reference to event-service — but the
    /// two constants must not drift. A silent mismatch fails closed, locking organisers out of
    /// their own invitations, which is an unpleasant thing to debug.
    /// </remarks>
    public const string EventsUpdate = "events.update";
}
