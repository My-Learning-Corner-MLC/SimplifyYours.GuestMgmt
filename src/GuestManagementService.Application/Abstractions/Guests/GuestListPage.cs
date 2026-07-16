using GuestManagementService.Domain.Guests;

namespace GuestManagementService.Application.Abstractions.Guests;

public sealed record GuestListPage(
    IReadOnlyCollection<Guest> Items,
    int PageNumber,
    int PageSize,
    int TotalCount);
