namespace GuestManagementService.Contracts.Seating;

public sealed record UpdateAreaRequest(
    Guid EventId,
    string? Name,
    string? Kind,
    string? Shape,
    double Width,
    double Height,
    string? Color,
    int? Capacity);
