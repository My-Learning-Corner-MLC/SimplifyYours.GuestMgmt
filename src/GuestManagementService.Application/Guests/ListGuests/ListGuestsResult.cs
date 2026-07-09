using GuestManagementService.Application.Guests;

namespace GuestManagementService.Application.Guests.ListGuests;

public sealed record ListGuestsResult(
    ListGuestsStatus Status,
    IReadOnlyList<GuestDetails> Guests)
{
    public static ListGuestsResult Found(IReadOnlyList<GuestDetails> guests)
    {
        return new ListGuestsResult(ListGuestsStatus.Found, guests);
    }

    public static ListGuestsResult EventNotFound()
    {
        return new ListGuestsResult(ListGuestsStatus.EventNotFound, Array.Empty<GuestDetails>());
    }
}
