using GuestManagementService.Application.Authorization;
using MediatR;

namespace GuestManagementService.Application.Seating.CreateArea;

public sealed record CreateAreaCommand(
    Guid EventId,
    string? Name,
    string? Kind,
    string? Shape,
    double Width,
    double Height,
    string? Color,
    int? Capacity) : BaseCommand, IRequest<CreateAreaResult>;
