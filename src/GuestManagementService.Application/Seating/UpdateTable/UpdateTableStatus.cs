namespace GuestManagementService.Application.Seating.UpdateTable;

public enum UpdateTableStatus
{
    Updated,
    EventNotFound,
    TableNotFound,
    SeatCountBelowOccupiedSeats
}
