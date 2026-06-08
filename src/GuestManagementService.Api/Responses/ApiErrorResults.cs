using Microsoft.AspNetCore.Mvc;

namespace GuestManagementService.Api.Responses;

internal static class ApiErrorResults
{
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
}
