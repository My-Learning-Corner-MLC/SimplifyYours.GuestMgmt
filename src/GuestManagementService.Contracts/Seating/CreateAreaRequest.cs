namespace GuestManagementService.Contracts.Seating;

public sealed record CreateAreaRequest(
    Guid EventId,
    string? Name,
    string? Kind,
    string? Shape,
    double Width,
    double Height,
    string? Color,
    int? Capacity);
