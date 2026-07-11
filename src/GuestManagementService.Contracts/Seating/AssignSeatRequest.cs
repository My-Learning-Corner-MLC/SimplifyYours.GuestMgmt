namespace GuestManagementService.Contracts.Seating;

public sealed record AssignSeatRequest(Guid EventId, Guid GuestId);
