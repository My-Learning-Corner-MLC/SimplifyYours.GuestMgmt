using GuestManagementService.Application.Abstractions.Common;
using GuestManagementService.Application.Abstractions.EventReferences;
using GuestManagementService.Application.Abstractions.Seating;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GuestManagementService.Application.Seating.CreateArea;

public sealed class CreateAreaCommandHandler(
    IEventReferenceRepository eventReferenceRepository,
    ISeatingLayoutProvisioner seatingLayoutProvisioner,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<CreateAreaCommandHandler> logger)
    : IRequestHandler<CreateAreaCommand, CreateAreaResult>
{
    public async Task<CreateAreaResult> Handle(CreateAreaCommand request, CancellationToken cancellationToken)
    {
        var currentUser = request.CurrentUser;

        var eventReference = await eventReferenceRepository.GetByIdAsync(request.EventId, cancellationToken);

        if (eventReference is null || eventReference.IsDeleted || eventReference.TenantId != currentUser.TenantId)
        {
            logger.LogWarning(
                "Area creation requested but event reference was not available. EventId: {EventId}.",
                request.EventId);
            return CreateAreaResult.EventNotFound();
        }

        var layout = await seatingLayoutProvisioner.GetOrCreateAsync(request.EventId, currentUser.TenantId, cancellationToken);

        SeatingParsing.TryParseAreaKind(request.Kind, out var kind);
        SeatingParsing.TryParseAreaShape(request.Shape, out var shape);
        var now = timeProvider.GetUtcNow();

        var area = layout.AddArea(
            Guid.NewGuid(),
            request.Name!.Trim(),
            kind,
            shape,
            request.Width,
            request.Height,
            request.Color,
            request.Capacity,
            now);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Area created for event {EventId}. AreaId: {AreaId}.",
            request.EventId,
            area.Id);

        return CreateAreaResult.Created(SeatingAreaDetails.From(area));
    }
}
