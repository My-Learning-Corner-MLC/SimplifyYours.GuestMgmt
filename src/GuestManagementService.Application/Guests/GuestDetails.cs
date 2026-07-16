using GuestManagementService.Application.Guests.Wedding;
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
    string? Relationship,
    string? Side,
    int PlusOnes,
    string? DietaryNotes,
    DateTimeOffset CreatedAt)
{
    public static GuestDetails From(Guest guest)
    {
        var metadata = WeddingGuestMetadataMapper.Deserialize(guest.Metadata);

        return new GuestDetails(
            guest.Id,
            guest.EventId,
            guest.FirstName,
            guest.LastName,
            guest.PhoneNumber,
            guest.EmailAddress,
            GuestParsing.ToContractValue(guest.Gender),
            WeddingGuestMetadataMapper.ToContractValue(metadata.Relationship),
            WeddingGuestMetadataMapper.ToContractValue(metadata.Side),
            metadata.PlusOnes,
            metadata.DietaryNotes,
            guest.CreatedAt);
    }
}
