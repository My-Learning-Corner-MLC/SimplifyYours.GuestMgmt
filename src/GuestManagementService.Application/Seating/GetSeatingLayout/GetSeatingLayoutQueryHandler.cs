using GuestManagementService.Application.Abstractions.Common;
using GuestManagementService.Application.Abstractions.EventReferences;
using GuestManagementService.Application.Abstractions.Guests;
using GuestManagementService.Application.Abstractions.Seating;
using GuestManagementService.Domain.Seating;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GuestManagementService.Application.Seating.GetSeatingLayout;

public sealed class GetSeatingLayoutQueryHandler(
    IEventReferenceRepository eventReferenceRepository,
    ISeatingLayoutRepository seatingLayoutRepository,
    IGuestRepository guestRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<GetSeatingLayoutQueryHandler> logger)
    : IRequestHandler<GetSeatingLayoutQuery, GetSeatingLayoutResult>
{
    public async Task<GetSeatingLayoutResult> Handle(GetSeatingLayoutQuery request, CancellationToken cancellationToken)
    {
        var currentUser = request.CurrentUser;

        var eventReference = await eventReferenceRepository.GetByIdAsync(request.EventId, cancellationToken);

        if (eventReference is null || eventReference.IsDeleted)
        {
            logger.LogWarning(
                "Seating layout requested but event reference was not available. EventId: {EventId}.",
                request.EventId);
            return GetSeatingLayoutResult.EventNotFound();
        }

        if (eventReference.TenantId != currentUser.TenantId)
        {
            logger.LogWarning(
                "Seating layout requested but event reference is owned by another tenant. EventId: {EventId}.",
                request.EventId);
            return GetSeatingLayoutResult.EventNotFound();
        }

        var layout = await seatingLayoutRepository.GetByEventAsync(request.EventId, currentUser.TenantId, cancellationToken);

        if (layout is null)
        {
            layout = SeatingLayout.Create(Guid.NewGuid(), request.EventId, currentUser.TenantId, timeProvider.GetUtcNow());
            await seatingLayoutRepository.AddAsync(layout, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Seating layout created. SeatingLayoutId: {SeatingLayoutId}. EventId: {EventId}.",
                layout.Id,
                request.EventId);
        }

        var guests = await guestRepository.ListByEventAsync(request.EventId, cancellationToken);
        var details = SeatingLayoutProjector.Project(layout, guests);

        return GetSeatingLayoutResult.Found(details);
    }
}
