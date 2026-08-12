namespace GuestManagementService.Application.Invitations;

/// <summary>
/// Computes when RSVPs close for an event.
/// </summary>
/// <remarks>
/// Derived rather than stored: the rule is fixed at the end of the day before the event, so the
/// organiser has a full clear day to work from final numbers. An organiser-configurable deadline
/// can arrive later without changing callers.
/// <para>
/// The date comes from the replicated event record, never from the invitation's saved
/// <c>eventDate</c> — that one is free text an organiser typed ("Saturday, September 12, 2026"),
/// which cannot be parsed reliably enough to close responses on.
/// </para>
/// <para>
/// The same deadline governs both the first submission and later edits. Letting a guest change
/// their answer after the deadline would defeat the point of having one — an organiser who has
/// already confirmed catering numbers should not see them move.
/// </para>
/// </remarks>
public static class RsvpDeadline
{
    /// <summary>
    /// End of the day before the event, in the event's time zone, expressed as UTC. Returns null
    /// when the event has no date, in which case RSVPs never close.
    /// </summary>
    public static DateTimeOffset? Compute(DateOnly? eventDate, string? timeZoneId)
    {
        if (eventDate is null)
        {
            return null;
        }

        var localEndOfDay = eventDate.Value.AddDays(-1).ToDateTime(new TimeOnly(23, 59, 59));
        var zone = ResolveZone(timeZoneId);

        // The event's zone, never the guest's or the server's: a wedding in Ho Chi Minh City closes
        // on Ho Chi Minh City's clock no matter where the guest opens the link from.
        var offset = zone.GetUtcOffset(localEndOfDay);

        return new DateTimeOffset(localEndOfDay, offset).ToUniversalTime();
    }

    public static bool IsOpen(DateOnly? eventDate, string? timeZoneId, DateTimeOffset now)
    {
        var deadline = Compute(eventDate, timeZoneId);

        return deadline is null || now <= deadline.Value;
    }

    private static TimeZoneInfo ResolveZone(string? timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            return TimeZoneInfo.Utc;
        }

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (Exception exception) when (exception is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            // Reference data replicated from another service can hold a zone this host does not
            // know. Falling back to UTC keeps invitations readable; throwing would take the whole
            // public page down over a display detail.
            return TimeZoneInfo.Utc;
        }
    }
}
