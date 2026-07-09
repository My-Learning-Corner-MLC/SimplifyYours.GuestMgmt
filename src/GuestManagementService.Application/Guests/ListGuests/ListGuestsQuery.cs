using GuestManagementService.Application.Authorization;
using MediatR;

namespace GuestManagementService.Application.Guests.ListGuests;

public sealed record ListGuestsQuery(Guid EventId) : BaseCommand, IRequest<ListGuestsResult>;
