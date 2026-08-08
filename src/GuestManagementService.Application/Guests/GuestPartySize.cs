using GuestManagementService.Application.Guests.Wedding;
using GuestManagementService.Domain.Guests;

namespace GuestManagementService.Application.Guests;

// How many accompanying attendees a guest brings — drives how many adjacent seats get
// reserved alongside their own when they're dropped onto one (see Seating.AssignSeat).
// Currently backed by the wedding metadata shape, the only one that exists today; as other
// event types define their own metadata, this becomes a lookup by event type.
public static class GuestPartySize
{
    public static int AccompanyingGuestCount(Guest guest)
    {
        return WeddingGuestMetadataMapper.Deserialize(guest.Metadata).PlusOnes;
    }
}
