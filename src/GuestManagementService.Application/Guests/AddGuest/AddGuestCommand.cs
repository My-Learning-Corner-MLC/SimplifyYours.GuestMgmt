using GuestManagementService.Application.Authorization;
using MediatR;

namespace GuestManagementService.Application.Guests.AddGuest;

public sealed record AddGuestCommand(
    Guid EventId,
    string? FirstName,
    string? LastName,
    string? PhoneNumber,
    string? EmailAddress,
    string? Gender,
    CurrentUser? CurrentUser = null)
    : IRequest<AddGuestResult>;
