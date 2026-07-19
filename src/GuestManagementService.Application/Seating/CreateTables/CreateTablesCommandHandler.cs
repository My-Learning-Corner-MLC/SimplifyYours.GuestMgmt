using GuestManagementService.Application.Abstractions.Common;
using GuestManagementService.Application.Abstractions.EventReferences;
using GuestManagementService.Application.Abstractions.Seating;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GuestManagementService.Application.Seating.CreateTables;

public sealed class CreateTablesCommandHandler(
    IEventReferenceRepository eventReferenceRepository,
    ISeatingLayoutProvisioner seatingLayoutProvisioner,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<CreateTablesCommandHandler> logger)
    : IRequestHandler<CreateTablesCommand, CreateTablesResult>
{
    public async Task<CreateTablesResult> Handle(CreateTablesCommand request, CancellationToken cancellationToken)
    {
        var currentUser = request.CurrentUser;

        var eventReference = await eventReferenceRepository.GetByIdAsync(request.EventId, cancellationToken);

        if (eventReference is null || eventReference.IsDeleted || eventReference.TenantId != currentUser.TenantId)
        {
            logger.LogWarning(
                "Table creation requested but event reference was not available. EventId: {EventId}.",
                request.EventId);
            return CreateTablesResult.EventNotFound();
        }

        var layout = await seatingLayoutProvisioner.GetOrCreateAsync(request.EventId, currentUser.TenantId, cancellationToken);

        SeatingParsing.TryParseShape(request.Shape, out var shape);
        var name = request.Name!.Trim();
        var now = timeProvider.GetUtcNow();

        var tables = Enumerable.Range(1, request.Count)
            .Select(index => layout.AddTable(
                Guid.NewGuid(),
                request.Count == 1 ? name : $"{name} · {index}",
                shape,
                request.SeatCount,
                now))
            .ToList();

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "{Count} table(s) created for event {EventId}.",
            tables.Count,
            request.EventId);

        return CreateTablesResult.Created(tables.Select(table => SeatingTableDetails.From(table)).ToList());
    }
}
