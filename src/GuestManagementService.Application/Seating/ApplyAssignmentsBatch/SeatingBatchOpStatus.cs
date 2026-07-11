namespace GuestManagementService.Application.Seating.ApplyAssignmentsBatch;

public enum SeatingBatchOpStatus
{
    Applied,
    Conflict,
    TableNotFound,
    GuestNotFound,
    SeatIndexOutOfRange
}
