namespace GuestManagementService.Application.Guests.AddGuest;

public sealed record AddGuestResult(
    AddGuestStatus Status,
    GuestDetails? Guest)
{
    public static AddGuestResult Created(GuestDetails guest)
    {
        return new AddGuestResult(AddGuestStatus.Created, guest);
    }

    public static AddGuestResult EventNotFound()
    {
        return new AddGuestResult(AddGuestStatus.EventNotFound, null);
    }

    public static AddGuestResult Duplicate()
    {
        return new AddGuestResult(AddGuestStatus.Duplicate, null);
    }
}
