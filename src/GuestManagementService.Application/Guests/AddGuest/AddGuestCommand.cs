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
    string? Relationship = null,
    string? Side = null,
    int? PlusOnes = null,
    string? DietaryNotes = null) : BaseCommand, IRequest<AddGuestResult>;
