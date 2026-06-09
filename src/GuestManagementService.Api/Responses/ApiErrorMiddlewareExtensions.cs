using Microsoft.AspNetCore.Diagnostics;

namespace GuestManagementService.Api.Responses;

internal static class ApiErrorMiddlewareExtensions
{
    public static WebApplication UseFriendlyErrorResponses(this WebApplication app)
    {
        app.UseExceptionHandler(errorApp =>
        {
            errorApp.Run(async context =>
            {
                var exception = context.Features.Get<IExceptionHandlerPathFeature>()?.Error;
                var statusCode = exception is BadHttpRequestException badRequestException
                    ? badRequestException.StatusCode
                    : StatusCodes.Status500InternalServerError;
                var title = statusCode == StatusCodes.Status400BadRequest
                    ? "Please check your request."
                    : "Something went wrong.";
                var message = statusCode == StatusCodes.Status400BadRequest
                    ? "The request could not be read. Please check the request format and try again."
                    : "Something went wrong while processing your request. Please try again later.";

                context.Response.StatusCode = statusCode;
                context.Response.ContentType = "application/problem+json";

                await context.Response.WriteAsJsonAsync(
                    ApiErrorResults.CreateProblemDetails(context, statusCode, title, message));
            });
        });

        app.UseStatusCodePages(async context =>
        {
            var httpContext = context.HttpContext;

            if (httpContext.Response.HasStarted
                || httpContext.Response.ContentLength is not null
                || httpContext.Response.ContentType is not null)
            {
                return;
            }

            var statusCode = httpContext.Response.StatusCode;
            var (title, message) = statusCode switch
            {
                StatusCodes.Status404NotFound => (
                    "We could not find that resource.",
                    "The requested resource was not found. Please check the URL and try again."),
                StatusCodes.Status405MethodNotAllowed => (
                    "That action is not available.",
                    "The requested action is not supported for this URL."),
                _ => (
                    "The request could not be completed.",
                    "The request could not be completed. Please check it and try again.")
            };

            httpContext.Response.ContentType = "application/problem+json";

            await httpContext.Response.WriteAsJsonAsync(
                ApiErrorResults.CreateProblemDetails(httpContext, statusCode, title, message));
        });

        return app;
    }
}
