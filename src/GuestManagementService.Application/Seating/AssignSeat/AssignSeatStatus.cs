namespace GuestManagementService.Application.Seating.AssignSeat;

public enum AssignSeatStatus
{
    Assigned,
    EventNotFound,
    TableNotFound,
    GuestNotFound,
    SeatIndexOutOfRange,
    SeatOccupied
}
