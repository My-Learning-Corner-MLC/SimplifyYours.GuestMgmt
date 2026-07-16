using FluentValidation;
using GuestManagementService.Api.Responses;
using GuestManagementService.Api.Security;
using GuestManagementService.Application.Guests;
using GuestManagementService.Application.Guests.AddGuest;
using GuestManagementService.Application.Guests.ListGuests;
using GuestManagementService.Contracts.Guests;
using MediatR;

namespace GuestManagementService.Api.Endpoints;

public static class GuestEndpoints
{
    public static IEndpointRouteBuilder MapGuestEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints
            .MapPost("/guest", AddGuestAsync)
            .WithName("AddGuest")
            .WithTags("Guests")
            .RequireAuthorization(Permissions.GuestsAdd);

        endpoints
            .MapPost("/guests/query", ListGuestsAsync)
            .WithName("QueryGuests")
            .WithTags("Guests")
            .RequireAuthorization(Permissions.GuestsView);

        return endpoints;
    }

    private static async Task<IResult> AddGuestAsync(
        AddGuestRequest request,
        HttpContext httpContext,
        ISender sender,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        try
        {
            if (request.GuestInfo is null)
            {
                return ApiErrorResults.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["GuestInfo"] = ["Guest info is required."]
                }, httpContext);
            }

            var result = await sender.Send(
                new AddGuestCommand(
                    request.EventId,
                    request.GuestInfo.FirstName,
                    request.GuestInfo.LastName,
                    request.GuestInfo.PhoneNumber,
                    request.GuestInfo.EmailAddress,
                    request.GuestInfo.Gender,
                    request.GuestInfo.Relationship,
                    request.GuestInfo.Side,
                    request.GuestInfo.PlusOnes,
                    request.GuestInfo.DietaryNotes),
                cancellationToken);

            return result.Status switch
            {
                AddGuestStatus.Created when result.Guest is not null => Created(result.Guest, loggerFactory),
                AddGuestStatus.EventNotFound => ApiErrorResults.NotFound(
                    "The event was not found. It may have been deleted or the id may be incorrect.",
                    httpContext),
                AddGuestStatus.Duplicate => ApiErrorResults.Conflict(
                    "This event already has a guest with the same phone number or email address.",
                    httpContext),
                _ => ApiErrorResults.Unexpected(
                    "The guest could not be added right now. Please try again later.",
                    httpContext)
            };
        }
        catch (ValidationException exception)
        {
            return ApiErrorResults.ValidationProblem(ToValidationErrors(exception), httpContext);
        }
    }

    private static async Task<IResult> ListGuestsAsync(
        QueryGuestsRequest request,
        HttpContext httpContext,
        ISender sender,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await sender.Send(
                new ListGuestsQuery(
                    request.EventId,
                    request.PageNumber,
                    request.PageSize,
                    request.Search,
                    request.SortBy,
                    request.SortDirection),
                cancellationToken);

            return result.Status switch
            {
                ListGuestsStatus.Found => Results.Ok(
                    new QueryGuestsResponse(
                        request.EventId,
                        result.Guests.Select(ToListItem).ToList(),
                        result.PageNumber,
                        result.PageSize,
                        result.TotalCount,
                        result.TotalPages,
                        result.HasPreviousPage,
                        result.HasNextPage)),
                ListGuestsStatus.EventNotFound => ApiErrorResults.NotFound(
                    "The event was not found. It may have been deleted or the id may be incorrect.",
                    httpContext),
                _ => ApiErrorResults.Unexpected(
                    "The guest list could not be loaded right now. Please try again later.",
                    httpContext)
            };
        }
        catch (ValidationException exception)
        {
            return ApiErrorResults.ValidationProblem(ToValidationErrors(exception), httpContext);
        }
    }

    private static IResult Created(
        GuestDetails guest,
        ILoggerFactory loggerFactory)
    {
        loggerFactory
            .CreateLogger("GuestManagementService.Guests")
            .LogInformation(
                "Guest created for event {EventId} with guest id {GuestId}.",
                guest.EventId,
                guest.Id);

        var response = new AddGuestResponse(
            guest.Id,
            guest.EventId,
            new GuestInfoResponse(
                guest.FirstName,
                guest.LastName,
                guest.PhoneNumber,
                guest.EmailAddress,
                guest.Gender,
                guest.Relationship,
                guest.Side,
                guest.PlusOnes,
                guest.DietaryNotes),
            guest.CreatedAt);

        return Results.Created($"/guest/{response.Id}", response);
    }

    private static GuestListItemResponse ToListItem(GuestDetails guest)
    {
        return new GuestListItemResponse(
            guest.Id,
            guest.FirstName,
            guest.LastName,
            guest.PhoneNumber,
            guest.EmailAddress,
            guest.Gender,
            guest.Relationship,
            guest.Side,
            guest.PlusOnes,
            guest.DietaryNotes,
            guest.CreatedAt);
    }

    private static Dictionary<string, string[]> ToValidationErrors(ValidationException exception)
    {
        return exception.Errors
            .GroupBy(error => error.PropertyName)
            .ToDictionary(
                group => group.Key,
                group => group.Select(error => error.ErrorMessage).ToArray());
    }
}
