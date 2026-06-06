using FluentValidation;
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
            .WithTags("Guests");

        return endpoints;
    }

    private static async Task<IResult> AddGuestAsync(
        AddGuestRequest request,
        ISender sender,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        try
        {
            if (request.GuestInfo is null)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["GuestInfo"] = ["Guest info is required."]
                });
            }

            var result = await sender.Send(
                new AddGuestCommand(
                    request.EventId,
                    request.GuestInfo.FirstName,
                    request.GuestInfo.LastName,
                    request.GuestInfo.PhoneNumber,
                    request.GuestInfo.EmailAddress,
                    request.GuestInfo.Gender),
                cancellationToken);

            return result.Status switch
            {
                AddGuestStatus.Created when result.Guest is not null => Created(result.Guest, loggerFactory),
                AddGuestStatus.EventNotFound => Results.NotFound(),
                AddGuestStatus.Duplicate => Results.Conflict(),
                _ => Results.Problem("Unexpected add guest result.")
            };
        }
        catch (ValidationException exception)
        {
            return Results.ValidationProblem(ToValidationErrors(exception));
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
