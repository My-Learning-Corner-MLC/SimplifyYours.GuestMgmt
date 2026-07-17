using GuestManagementService.Application.Authorization;
using MediatR;

namespace GuestManagementService.Application.Guests.ListGuests;

public sealed record ListGuestsQuery(
    Guid EventId,
    int? PageNumber = null,
    int? PageSize = null,
    string? Search = null,
    string? SortBy = null,
    string? SortDirection = null) : BaseCommand, IRequest<ListGuestsResult>;
