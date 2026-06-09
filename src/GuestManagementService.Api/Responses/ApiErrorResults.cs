using Microsoft.AspNetCore.Mvc;

namespace GuestManagementService.Api.Responses;

internal static class ApiErrorResults
{
    public static IResult ValidationProblem(IDictionary<string, string[]> errors, HttpContext? context = null)
    {
        const string message = "Some information is missing or invalid. Please check the highlighted fields and try again.";

        return Results.ValidationProblem(
            errors,
            statusCode: StatusCodes.Status400BadRequest,
            title: "Please check your request.",
            detail: message,
            extensions: CreateExtensions(message, context));
    }

    public static IResult NotFound(string message, HttpContext? context = null)
    {
        return Problem(StatusCodes.Status404NotFound, "We could not find that resource.", message, context);
    }

    public static IResult Conflict(string message, HttpContext? context = null)
    {
        return Problem(StatusCodes.Status409Conflict, "This request conflicts with the current data.", message, context);
    }

    public static IResult Unexpected(string message, HttpContext? context = null)
    {
        return Problem(StatusCodes.Status500InternalServerError, "Something went wrong.", message, context);
    }

    public static ProblemDetails CreateProblemDetails(HttpContext context, int statusCode, string title, string message)
    {
        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = message,
            Instance = context.Request.Path
        };

        problemDetails.Extensions["message"] = message;
        problemDetails.Extensions["correlationId"] = CorrelationId.Get(context);

        return problemDetails;
    }

    private static IResult Problem(int statusCode, string title, string message, HttpContext? context)
    {
        return Results.Problem(
            statusCode: statusCode,
            title: title,
            detail: message,
            extensions: CreateExtensions(message, context));
    }

    private static Dictionary<string, object?> CreateExtensions(string message, HttpContext? context)
    {
        var extensions = new Dictionary<string, object?>
        {
            ["message"] = message
        };

        if (context is not null)
        {
            extensions["correlationId"] = CorrelationId.Get(context);
        }

        return extensions;
    }
}
