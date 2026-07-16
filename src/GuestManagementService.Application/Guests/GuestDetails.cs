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
    object? EventMetadata,
    DateTimeOffset CreatedAt)
{
    /// <summary>
    /// Maps a stored guest to its contract-facing details. <paramref name="metadataMapper"/> is
    /// the mapper resolved for the guest's event type (<c>null</c> when the event type is
    /// unrecognized or unset) — <see cref="Guest.Metadata"/> is only interpreted when a mapper is
    /// available, never assumed.
    /// </summary>
    public static GuestDetails From(Guest guest, IGuestMetadataMapper? metadataMapper)
    {
        return new GuestDetails(
            guest.Id,
            guest.EventId,
            guest.FirstName,
            guest.LastName,
            guest.PhoneNumber,
            guest.EmailAddress,
            GuestParsing.ToContractValue(guest.Gender),
            metadataMapper?.ToContract(guest.Metadata),
            guest.CreatedAt);
    }
}
