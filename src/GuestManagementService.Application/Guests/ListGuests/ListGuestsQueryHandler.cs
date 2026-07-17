using GuestManagementService.Application.Abstractions.EventReferences;
using GuestManagementService.Application.Abstractions.Guests;
using GuestManagementService.Application.Guests;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GuestManagementService.Application.Guests.ListGuests;

public sealed class ListGuestsQueryHandler(
    IEventReferenceRepository eventReferenceRepository,
    IGuestRepository guestRepository,
    IGuestMetadataMapperFactory metadataMapperFactory,
    ILogger<ListGuestsQueryHandler> logger)
    : IRequestHandler<ListGuestsQuery, ListGuestsResult>
{
    public async Task<ListGuestsResult> Handle(ListGuestsQuery request, CancellationToken cancellationToken)
    {
        var currentUser = request.CurrentUser;

        var eventReference = await eventReferenceRepository.GetByIdAsync(request.EventId, cancellationToken);

        if (eventReference is null || eventReference.IsDeleted)
        {
            logger.LogWarning(
                "Guest list requested but event reference was not available. EventId: {EventId}.",
                request.EventId);
            return ListGuestsResult.EventNotFound();
        }

        if (eventReference.TenantId != currentUser.TenantId)
        {
            logger.LogWarning(
                "Guest list requested but event reference is owned by another tenant. EventId: {EventId}.",
                request.EventId);
            return ListGuestsResult.EventNotFound();
        }

        var options = new GuestListQueryOptions(
            request.EventId,
            currentUser.TenantId,
            request.PageNumber ?? ListGuestsQueryDefaults.PageNumber,
            request.PageSize ?? ListGuestsQueryDefaults.PageSize,
            NormalizeOptionalText(request.Search),
            ResolveSortBy(request.SortBy),
            ResolveSortDirection(request.SortDirection));

        var page = await guestRepository.ListAsync(options, cancellationToken);
        var totalPages = page.TotalCount == 0
            ? 0
            : (int)Math.Ceiling(page.TotalCount / (double)page.PageSize);

        var metadataMapper = metadataMapperFactory.Resolve(eventReference.EventType);
        var guests = page.Items.Select(guest => GuestDetails.From(guest, metadataMapper)).ToList();

        logger.LogInformation(
            "Guest list returned {ReturnedCount} of {TotalCount} guests for event {EventId}. PageNumber: {PageNumber}. PageSize: {PageSize}.",
            guests.Count,
            page.TotalCount,
            request.EventId,
            page.PageNumber,
            page.PageSize);

        return ListGuestsResult.Found(
            guests,
            page.PageNumber,
            page.PageSize,
            page.TotalCount,
            totalPages,
            page.PageNumber > 1,
            page.PageNumber < totalPages);
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static GuestSortField ResolveSortBy(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? GuestSortField.CreatedAt
            : value.Trim().ToLowerInvariant() switch
            {
                "name" => GuestSortField.Name,
                "email" => GuestSortField.Email,
                "createdat" => GuestSortField.CreatedAt,
                _ => throw new ArgumentException("Sort field must be one of: name, email, createdAt.", nameof(value))
            };
    }

    private static SortDirection ResolveSortDirection(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? SortDirection.Asc
            : Enum.Parse<SortDirection>(value, ignoreCase: true);
    }
}
