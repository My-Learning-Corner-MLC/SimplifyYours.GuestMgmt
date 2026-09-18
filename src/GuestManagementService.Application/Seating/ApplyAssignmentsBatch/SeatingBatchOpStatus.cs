namespace GuestManagementService.Application.Seating.ApplyAssignmentsBatch;

public enum SeatingBatchOpStatus
{
    Applied,
    Conflict,
    InsufficientAdjacentSeats,
    TableNotFound,
    GuestNotFound,
    SeatIndexOutOfRange
}
