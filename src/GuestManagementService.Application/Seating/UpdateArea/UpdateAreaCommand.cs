using GuestManagementService.Application.Authorization;
using MediatR;

namespace GuestManagementService.Application.Seating.UpdateArea;

public sealed record UpdateAreaCommand(
    Guid EventId,
    Guid AreaId,
    string? Name,
    string? Kind,
    string? Shape,
    double Width,
    double Height,
    string? Color,
    int? Capacity) : BaseCommand, IRequest<UpdateAreaResult>;
