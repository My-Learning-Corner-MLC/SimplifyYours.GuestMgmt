using MediatR;

namespace GuestManagementService.Application.Guests.AddGuest;

public sealed record AddGuestCommand(
    Guid EventId,
    string? FirstName,
    string? LastName,
    string? PhoneNumber,
    string? EmailAddress,
    string? Gender)
    : IRequest<AddGuestResult>;
