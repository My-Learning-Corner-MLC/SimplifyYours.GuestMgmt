using System.Text.Json;
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
    JsonElement? EventMetadata = null,
    IReadOnlyList<string>? Tags = null) : BaseCommand, IRequest<AddGuestResult>;
