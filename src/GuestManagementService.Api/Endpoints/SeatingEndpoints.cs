using FluentValidation;
using GuestManagementService.Api.Responses;
using GuestManagementService.Api.Security;
using GuestManagementService.Application.Seating;
using GuestManagementService.Application.Seating.GetSeatingLayout;
using GuestManagementService.Contracts.Seating;
using MediatR;

namespace GuestManagementService.Api.Endpoints;

public static class SeatingEndpoints
{
    public static IEndpointRouteBuilder MapSeatingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints
            .MapGet("/seating", GetSeatingLayoutAsync)
            .WithName("GetSeatingLayout")
            .WithTags("Seating")
            .RequireAuthorization(Permissions.SeatingView);

        return endpoints;
    }

    private static async Task<IResult> GetSeatingLayoutAsync(
        Guid eventId,
        HttpContext httpContext,
        ISender sender,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await sender.Send(new GetSeatingLayoutQuery(eventId), cancellationToken);

            return result.Status switch
            {
                GetSeatingLayoutStatus.Found when result.Layout is not null => Results.Ok(ToResponse(result.Layout)),
                GetSeatingLayoutStatus.EventNotFound => ApiErrorResults.NotFound(
                    "The event was not found. It may have been deleted or the id may be incorrect.",
                    httpContext),
                _ => ApiErrorResults.Unexpected(
                    "The seating layout could not be loaded right now. Please try again later.",
                    httpContext)
            };
        }
        catch (ValidationException exception)
        {
            return ApiErrorResults.ValidationProblem(ToValidationErrors(exception), httpContext);
        }
    }

    private static SeatingLayoutResponse ToResponse(SeatingLayoutDetails layout)
    {
        return new SeatingLayoutResponse(
            layout.EventId,
            layout.Tables.Select(ToTableResponse).ToList(),
            new SeatingSummaryResponse(
                layout.Summary.TableCount,
                layout.Summary.SeatCount,
                layout.Summary.SeatedCount,
                layout.Summary.FloatingCount));
    }

    private static SeatingTableResponse ToTableResponse(SeatingTableDetails table)
    {
        return new SeatingTableResponse(
            table.Id,
            table.Name,
            table.Shape,
            table.SeatCount,
            table.IsFull,
            table.PositionX,
            table.PositionY,
            table.Rotation);
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
