namespace GuestManagementService.Contracts.Guests;

public sealed record ListGuestsResponse(
    Guid EventId,
    IReadOnlyList<GuestListItemResponse> Guests);
