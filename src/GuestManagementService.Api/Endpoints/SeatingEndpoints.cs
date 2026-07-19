using FluentValidation;
using GuestManagementService.Api.Responses;
using GuestManagementService.Api.Security;
using GuestManagementService.Application.Seating;
using GuestManagementService.Application.Seating.ApplyAreaPositionsBatch;
using GuestManagementService.Application.Seating.ApplyAssignmentsBatch;
using GuestManagementService.Application.Seating.ApplyTablePositionsBatch;
using GuestManagementService.Application.Seating.AssignSeat;
using GuestManagementService.Application.Seating.CreateArea;
using GuestManagementService.Application.Seating.CreateTables;
using GuestManagementService.Application.Seating.DeleteArea;
using GuestManagementService.Application.Seating.DeleteTable;
using GuestManagementService.Application.Seating.GetSeatingLayout;
using GuestManagementService.Application.Seating.UnassignSeat;
using GuestManagementService.Application.Seating.UpdateArea;
using GuestManagementService.Application.Seating.UpdateAreaPosition;
using GuestManagementService.Application.Seating.UpdateTable;
using GuestManagementService.Application.Seating.UpdateTablePosition;
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

        endpoints
            .MapPut("/seating/tables/{tableId:guid}/seats/{seatIndex:int}", AssignSeatAsync)
            .WithName("AssignSeat")
            .WithTags("Seating")
            .RequireAuthorization(Permissions.SeatingManage);

        endpoints
            .MapDelete("/seating/tables/{tableId:guid}/seats/{seatIndex:int}", UnassignSeatAsync)
            .WithName("UnassignSeat")
            .WithTags("Seating")
            .RequireAuthorization(Permissions.SeatingManage);

        endpoints
            .MapPut("/seating/assignments", ApplyAssignmentsBatchAsync)
            .WithName("ApplyAssignmentsBatch")
            .WithTags("Seating")
            .RequireAuthorization(Permissions.SeatingManage);

        endpoints
            .MapPatch("/seating/tables/{tableId:guid}/position", UpdateTablePositionAsync)
            .WithName("UpdateTablePosition")
            .WithTags("Seating")
            .RequireAuthorization(Permissions.SeatingManage);

        endpoints
            .MapPatch("/seating/tables/positions", ApplyTablePositionsBatchAsync)
            .WithName("ApplyTablePositionsBatch")
            .WithTags("Seating")
            .RequireAuthorization(Permissions.SeatingManage);

        endpoints
            .MapPost("/seating/areas", CreateAreaAsync)
            .WithName("CreateArea")
            .WithTags("Seating")
            .RequireAuthorization(Permissions.SeatingManage);

        endpoints
            .MapPut("/seating/areas/{areaId:guid}", UpdateAreaAsync)
            .WithName("UpdateArea")
            .WithTags("Seating")
            .RequireAuthorization(Permissions.SeatingManage);

        endpoints
            .MapDelete("/seating/areas/{areaId:guid}", DeleteAreaAsync)
            .WithName("DeleteArea")
            .WithTags("Seating")
            .RequireAuthorization(Permissions.SeatingManage);

        endpoints
            .MapPatch("/seating/areas/{areaId:guid}/position", UpdateAreaPositionAsync)
            .WithName("UpdateAreaPosition")
            .WithTags("Seating")
            .RequireAuthorization(Permissions.SeatingManage);

        endpoints
            .MapPatch("/seating/areas/positions", ApplyAreaPositionsBatchAsync)
            .WithName("ApplyAreaPositionsBatch")
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
                UpdateTableStatus.SeatCountBelowOccupiedSeats => ApiErrorResults.Conflict(
                    "Seat count can't be lower than the highest occupied seat. Unseat some guests first.",
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

    private static async Task<IResult> AssignSeatAsync(
        Guid tableId,
        int seatIndex,
        AssignSeatRequest request,
        HttpContext httpContext,
        ISender sender,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await sender.Send(
                new AssignSeatCommand(request.EventId, tableId, seatIndex, request.GuestId),
                cancellationToken);

            return result.Status switch
            {
                AssignSeatStatus.Assigned when result.Table is not null => Results.Ok(ToTableResponse(result.Table)),
                AssignSeatStatus.EventNotFound => ApiErrorResults.NotFound(
                    "The event was not found. It may have been deleted or the id may be incorrect.",
                    httpContext),
                AssignSeatStatus.TableNotFound => ApiErrorResults.NotFound(
                    "The table was not found. It may have been deleted or the id may be incorrect.",
                    httpContext),
                AssignSeatStatus.GuestNotFound => ApiErrorResults.NotFound(
                    "The guest was not found for this event.",
                    httpContext),
                AssignSeatStatus.SeatIndexOutOfRange => ApiErrorResults.ValidationProblem(
                    new Dictionary<string, string[]> { ["SeatIndex"] = ["Seat index is outside this table's seat count."] },
                    httpContext),
                AssignSeatStatus.SeatOccupied => ApiErrorResults.Conflict(
                    "That seat is already taken. Someone else may have just been seated there.",
                    httpContext),
                AssignSeatStatus.InsufficientAdjacentSeats => ApiErrorResults.Conflict(
                    "There aren't enough free adjacent seats here for this guest's whole party. " +
                    "Try a different seat, or another table.",
                    httpContext),
                _ => ApiErrorResults.Unexpected(
                    "The seat could not be assigned right now. Please try again later.",
                    httpContext)
            };
        }
        catch (ValidationException exception)
        {
            return ApiErrorResults.ValidationProblem(ToValidationErrors(exception), httpContext);
        }
    }

    private static async Task<IResult> UnassignSeatAsync(
        Guid tableId,
        int seatIndex,
        Guid eventId,
        HttpContext httpContext,
        ISender sender,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await sender.Send(new UnassignSeatCommand(eventId, tableId, seatIndex), cancellationToken);

            return result.Status switch
            {
                UnassignSeatStatus.Unassigned => Results.NoContent(),
                UnassignSeatStatus.EventNotFound => ApiErrorResults.NotFound(
                    "The event was not found. It may have been deleted or the id may be incorrect.",
                    httpContext),
                UnassignSeatStatus.TableNotFound => ApiErrorResults.NotFound(
                    "The table was not found. It may have been deleted or the id may be incorrect.",
                    httpContext),
                _ => ApiErrorResults.Unexpected(
                    "The seat could not be unassigned right now. Please try again later.",
                    httpContext)
            };
        }
        catch (ValidationException exception)
        {
            return ApiErrorResults.ValidationProblem(ToValidationErrors(exception), httpContext);
        }
    }

    private static async Task<IResult> ApplyAssignmentsBatchAsync(
        ApplyAssignmentsBatchRequest request,
        HttpContext httpContext,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var opErrors = new Dictionary<string, string[]>();
        var ops = new List<SeatingBatchOpInput>(request.Ops.Count);

        for (var index = 0; index < request.Ops.Count; index++)
        {
            var opRequest = request.Ops[index];
            if (!SeatingParsing.TryParseBatchOpType(opRequest.Op, out var opType))
            {
                opErrors[$"Ops[{index}].Op"] = ["Op must be one of: Assign, Unassign."];
                continue;
            }

            ops.Add(new SeatingBatchOpInput(opType, opRequest.GuestId, opRequest.TableId, opRequest.SeatIndex));
        }

        if (opErrors.Count > 0)
        {
            return ApiErrorResults.ValidationProblem(opErrors, httpContext);
        }

        try
        {
            var result = await sender.Send(new ApplyAssignmentsBatchCommand(request.EventId, ops), cancellationToken);

            return result.Status switch
            {
                ApplyAssignmentsBatchStatus.Applied when result.Layout is not null => Results.Ok(
                    new ApplyAssignmentsBatchResponse(
                        ToResponse(result.Layout),
                        result.OpResults.Select(ToOpResponse).ToList())),
                ApplyAssignmentsBatchStatus.EventNotFound => ApiErrorResults.NotFound(
                    "The event was not found. It may have been deleted or the id may be incorrect.",
                    httpContext),
                _ => ApiErrorResults.Unexpected(
                    "The seating changes could not be saved right now. Please try again later.",
                    httpContext)
            };
        }
        catch (ValidationException exception)
        {
            return ApiErrorResults.ValidationProblem(ToValidationErrors(exception), httpContext);
        }
    }

    private static async Task<IResult> UpdateTablePositionAsync(
        Guid tableId,
        UpdateTablePositionRequest request,
        HttpContext httpContext,
        ISender sender,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await sender.Send(
                new UpdateTablePositionCommand(request.EventId, tableId, request.PositionX, request.PositionY, request.Rotation),
                cancellationToken);

            return result.Status switch
            {
                UpdateTablePositionStatus.Updated when result.Table is not null => Results.Ok(ToTableResponse(result.Table)),
                UpdateTablePositionStatus.EventNotFound => ApiErrorResults.NotFound(
                    "The event was not found. It may have been deleted or the id may be incorrect.",
                    httpContext),
                UpdateTablePositionStatus.TableNotFound => ApiErrorResults.NotFound(
                    "The table was not found. It may have been deleted or the id may be incorrect.",
                    httpContext),
                _ => ApiErrorResults.Unexpected(
                    "The table's position could not be saved right now. Please try again later.",
                    httpContext)
            };
        }
        catch (ValidationException exception)
        {
            return ApiErrorResults.ValidationProblem(ToValidationErrors(exception), httpContext);
        }
    }

    private static async Task<IResult> ApplyTablePositionsBatchAsync(
        ApplyTablePositionsBatchRequest request,
        HttpContext httpContext,
        ISender sender,
        CancellationToken cancellationToken)
    {
        try
        {
            var positions = request.Positions
                .Select(p => new TablePositionInput(p.TableId, p.PositionX, p.PositionY, p.Rotation))
                .ToList();
            var result = await sender.Send(new ApplyTablePositionsBatchCommand(request.EventId, positions), cancellationToken);

            return result.Status switch
            {
                ApplyTablePositionsBatchStatus.Applied => Results.Ok(
                    new ApplyTablePositionsBatchResponse(
                        result.Results
                            .Select(r => new TablePositionOpResponse(r.TableId, r.Status.ToString()))
                            .ToList())),
                ApplyTablePositionsBatchStatus.EventNotFound => ApiErrorResults.NotFound(
                    "The event was not found. It may have been deleted or the id may be incorrect.",
                    httpContext),
                _ => ApiErrorResults.Unexpected(
                    "The table positions could not be saved right now. Please try again later.",
                    httpContext)
            };
        }
        catch (ValidationException exception)
        {
            return ApiErrorResults.ValidationProblem(ToValidationErrors(exception), httpContext);
        }
    }

    private static async Task<IResult> CreateAreaAsync(
        CreateAreaRequest request,
        HttpContext httpContext,
        ISender sender,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await sender.Send(
                new CreateAreaCommand(request.EventId, request.Name, request.Kind, request.Shape, request.Width, request.Height, request.Color, request.Capacity),
                cancellationToken);

            return result.Status switch
            {
                CreateAreaStatus.Created when result.Area is not null => Results.Created("/seating", ToAreaResponse(result.Area)),
                CreateAreaStatus.EventNotFound => ApiErrorResults.NotFound(
                    "The event was not found. It may have been deleted or the id may be incorrect.",
                    httpContext),
                _ => ApiErrorResults.Unexpected(
                    "The area could not be created right now. Please try again later.",
                    httpContext)
            };
        }
        catch (ValidationException exception)
        {
            return ApiErrorResults.ValidationProblem(ToValidationErrors(exception), httpContext);
        }
    }

    private static async Task<IResult> UpdateAreaAsync(
        Guid areaId,
        UpdateAreaRequest request,
        HttpContext httpContext,
        ISender sender,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await sender.Send(
                new UpdateAreaCommand(request.EventId, areaId, request.Name, request.Kind, request.Shape, request.Width, request.Height, request.Color, request.Capacity),
                cancellationToken);

            return result.Status switch
            {
                UpdateAreaStatus.Updated when result.Area is not null => Results.Ok(ToAreaResponse(result.Area)),
                UpdateAreaStatus.EventNotFound => ApiErrorResults.NotFound(
                    "The event was not found. It may have been deleted or the id may be incorrect.",
                    httpContext),
                UpdateAreaStatus.AreaNotFound => ApiErrorResults.NotFound(
                    "The area was not found. It may have been deleted or the id may be incorrect.",
                    httpContext),
                _ => ApiErrorResults.Unexpected(
                    "The area could not be updated right now. Please try again later.",
                    httpContext)
            };
        }
        catch (ValidationException exception)
        {
            return ApiErrorResults.ValidationProblem(ToValidationErrors(exception), httpContext);
        }
    }

    private static async Task<IResult> DeleteAreaAsync(
        Guid areaId,
        Guid eventId,
        HttpContext httpContext,
        ISender sender,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await sender.Send(new DeleteAreaCommand(eventId, areaId), cancellationToken);

            return result.Status switch
            {
                DeleteAreaStatus.Deleted => Results.NoContent(),
                DeleteAreaStatus.EventNotFound => ApiErrorResults.NotFound(
                    "The event was not found. It may have been deleted or the id may be incorrect.",
                    httpContext),
                DeleteAreaStatus.AreaNotFound => ApiErrorResults.NotFound(
                    "The area was not found. It may have been deleted or the id may be incorrect.",
                    httpContext),
                _ => ApiErrorResults.Unexpected(
                    "The area could not be deleted right now. Please try again later.",
                    httpContext)
            };
        }
        catch (ValidationException exception)
        {
            return ApiErrorResults.ValidationProblem(ToValidationErrors(exception), httpContext);
        }
    }

    private static async Task<IResult> UpdateAreaPositionAsync(
        Guid areaId,
        UpdateAreaPositionRequest request,
        HttpContext httpContext,
        ISender sender,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await sender.Send(
                new UpdateAreaPositionCommand(request.EventId, areaId, request.PositionX, request.PositionY, request.Rotation),
                cancellationToken);

            return result.Status switch
            {
                UpdateAreaPositionStatus.Updated when result.Area is not null => Results.Ok(ToAreaResponse(result.Area)),
                UpdateAreaPositionStatus.EventNotFound => ApiErrorResults.NotFound(
                    "The event was not found. It may have been deleted or the id may be incorrect.",
                    httpContext),
                UpdateAreaPositionStatus.AreaNotFound => ApiErrorResults.NotFound(
                    "The area was not found. It may have been deleted or the id may be incorrect.",
                    httpContext),
                _ => ApiErrorResults.Unexpected(
                    "The area's position could not be saved right now. Please try again later.",
                    httpContext)
            };
        }
        catch (ValidationException exception)
        {
            return ApiErrorResults.ValidationProblem(ToValidationErrors(exception), httpContext);
        }
    }

    private static async Task<IResult> ApplyAreaPositionsBatchAsync(
        ApplyAreaPositionsBatchRequest request,
        HttpContext httpContext,
        ISender sender,
        CancellationToken cancellationToken)
    {
        try
        {
            var positions = request.Positions
                .Select(p => new AreaPositionInput(p.AreaId, p.PositionX, p.PositionY, p.Rotation))
                .ToList();
            var result = await sender.Send(new ApplyAreaPositionsBatchCommand(request.EventId, positions), cancellationToken);

            return result.Status switch
            {
                ApplyAreaPositionsBatchStatus.Applied => Results.Ok(
                    new ApplyAreaPositionsBatchResponse(
                        result.Results
                            .Select(r => new AreaPositionOpResponse(r.AreaId, r.Status.ToString()))
                            .ToList())),
                ApplyAreaPositionsBatchStatus.EventNotFound => ApiErrorResults.NotFound(
                    "The event was not found. It may have been deleted or the id may be incorrect.",
                    httpContext),
                _ => ApiErrorResults.Unexpected(
                    "The area positions could not be saved right now. Please try again later.",
                    httpContext)
            };
        }
        catch (ValidationException exception)
        {
            return ApiErrorResults.ValidationProblem(ToValidationErrors(exception), httpContext);
        }
    }

    private static SeatingBatchOpResponse ToOpResponse(SeatingBatchOpResult result)
    {
        return new SeatingBatchOpResponse(result.GuestId, result.Status.ToString());
    }

    private static SeatingLayoutResponse ToResponse(SeatingLayoutDetails layout)
    {
        return new SeatingLayoutResponse(
            layout.EventId,
            layout.Tables.Select(ToTableResponse).ToList(),
            layout.Areas.Select(ToAreaResponse).ToList(),
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
            table.Rotation,
            table.Seats
                .Select(seat => new SeatingSeatResponse(
                    seat.SeatIndex, seat.GuestId, seat.GuestName, seat.IsReservedForParty, seat.PartyOwnerGuestId))
                .ToList());
    }

    private static SeatingAreaResponse ToAreaResponse(SeatingAreaDetails area)
    {
        return new SeatingAreaResponse(
            area.Id,
            area.Name,
            area.Kind,
            area.Shape,
            area.Width,
            area.Height,
            area.PositionX,
            area.PositionY,
            area.Rotation,
            area.Color,
            area.Capacity);
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
