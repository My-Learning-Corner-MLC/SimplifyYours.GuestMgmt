using GuestManagementService.Domain.Guests;

namespace GuestManagementService.Application.Guests;

public sealed record GuestDetails(
    Guid Id,
    Guid EventId,
    string FirstName,
    string LastName,
    string PhoneNumber,
    string? EmailAddress,
    string Gender,
    DateTimeOffset CreatedAt)
{
    public static GuestDetails From(Guest guest)
    {
        return new GuestDetails(
            guest.Id,
            guest.EventId,
            guest.FirstName,
            guest.LastName,
            guest.PhoneNumber,
            guest.EmailAddress,
            GuestParsing.ToContractValue(guest.Gender),
            guest.CreatedAt);
    }
}
