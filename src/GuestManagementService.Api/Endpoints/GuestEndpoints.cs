using FluentValidation;
using GuestManagementService.Api.Responses;
using GuestManagementService.Api.Security;
using GuestManagementService.Application.Guests.AddGuest;
using GuestManagementService.Contracts.Guests;
using MediatR;

namespace GuestManagementService.Api.Endpoints;

internal static class GuestEndpoints
{
    public static IEndpointRouteBuilder MapGuestEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints
            .MapPost("/guest", AddGuestAsync)
            .WithName("AddGuest")
            .WithTags("Guests")
            .RequireAuthorization();

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
            if (!CurrentUserResolver.TryResolve(httpContext.User, out var currentUser))
            {
                return Results.Challenge();
            }

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
                    currentUser),
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

    private static IResult Created(
        Application.Guests.GuestDetails guest,
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
                guest.Gender),
            guest.CreatedAt);

        return Results.Created($"/guest/{response.Id}", response);
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
