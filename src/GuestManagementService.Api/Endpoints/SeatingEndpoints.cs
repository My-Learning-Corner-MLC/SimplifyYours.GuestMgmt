using FluentValidation;
using GuestManagementService.Api.Responses;
using GuestManagementService.Api.Security;
using GuestManagementService.Application.Seating;
using GuestManagementService.Application.Seating.CreateTables;
using GuestManagementService.Application.Seating.DeleteTable;
using GuestManagementService.Application.Seating.GetSeatingLayout;
using GuestManagementService.Application.Seating.UpdateTable;
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

        endpoints
            .MapPost("/seating/tables", CreateTablesAsync)
            .WithName("CreateTables")
            .WithTags("Seating")
            .RequireAuthorization(Permissions.SeatingManage);

        endpoints
            .MapPut("/seating/tables/{tableId:guid}", UpdateTableAsync)
            .WithName("UpdateTable")
            .WithTags("Seating")
            .RequireAuthorization(Permissions.SeatingManage);

        endpoints
            .MapDelete("/seating/tables/{tableId:guid}", DeleteTableAsync)
            .WithName("DeleteTable")
            .WithTags("Seating")
            .RequireAuthorization(Permissions.SeatingManage);

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

    private static async Task<IResult> CreateTablesAsync(
        CreateTablesRequest request,
        HttpContext httpContext,
        ISender sender,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await sender.Send(
                new CreateTablesCommand(request.EventId, request.Name, request.Shape, request.SeatCount, request.Count),
                cancellationToken);

            return result.Status switch
            {
                CreateTablesStatus.Created => Results.Created(
                    "/seating",
                    new CreateTablesResponse(result.Tables.Select(ToTableResponse).ToList())),
                CreateTablesStatus.EventNotFound => ApiErrorResults.NotFound(
                    "The event was not found. It may have been deleted or the id may be incorrect.",
                    httpContext),
                _ => ApiErrorResults.Unexpected(
                    "The table(s) could not be created right now. Please try again later.",
                    httpContext)
            };
        }
        catch (ValidationException exception)
        {
            return ApiErrorResults.ValidationProblem(ToValidationErrors(exception), httpContext);
        }
    }

    private static async Task<IResult> UpdateTableAsync(
        Guid tableId,
        UpdateTableRequest request,
        HttpContext httpContext,
        ISender sender,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await sender.Send(
                new UpdateTableCommand(request.EventId, tableId, request.Name, request.Shape, request.SeatCount, request.IsFull),
                cancellationToken);

            return result.Status switch
            {
                UpdateTableStatus.Updated when result.Table is not null => Results.Ok(ToTableResponse(result.Table)),
                UpdateTableStatus.EventNotFound => ApiErrorResults.NotFound(
                    "The event was not found. It may have been deleted or the id may be incorrect.",
                    httpContext),
                UpdateTableStatus.TableNotFound => ApiErrorResults.NotFound(
                    "The table was not found. It may have been deleted or the id may be incorrect.",
                    httpContext),
                _ => ApiErrorResults.Unexpected(
                    "The table could not be updated right now. Please try again later.",
                    httpContext)
            };
        }
        catch (ValidationException exception)
        {
            return ApiErrorResults.ValidationProblem(ToValidationErrors(exception), httpContext);
        }
    }

    private static async Task<IResult> DeleteTableAsync(
        Guid tableId,
        Guid eventId,
        HttpContext httpContext,
        ISender sender,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await sender.Send(new DeleteTableCommand(eventId, tableId), cancellationToken);

            return result.Status switch
            {
                DeleteTableStatus.Deleted => Results.NoContent(),
                DeleteTableStatus.EventNotFound => ApiErrorResults.NotFound(
                    "The event was not found. It may have been deleted or the id may be incorrect.",
                    httpContext),
                DeleteTableStatus.TableNotFound => ApiErrorResults.NotFound(
                    "The table was not found. It may have been deleted or the id may be incorrect.",
                    httpContext),
                _ => ApiErrorResults.Unexpected(
                    "The table could not be deleted right now. Please try again later.",
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
