using System.Diagnostics;
using GuestManagementService.Api.Responses;

namespace GuestManagementService.Api.Middleware;

internal sealed class RequestLoggingMiddleware(
    RequestDelegate next,
    ILogger<RequestLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = ResolveCorrelationId(context);
        context.Items[CorrelationId.HeaderName] = correlationId;
        context.Response.Headers[CorrelationId.HeaderName] = correlationId;

        var stopwatch = Stopwatch.StartNew();

        logger.LogInformation(
            "HTTP request started. Method: {Method}. Path: {Path}. CorrelationId: {CorrelationId}. TraceId: {TraceId}.",
            context.Request.Method,
            context.Request.Path,
            correlationId,
            context.TraceIdentifier);

        try
        {
            await next(context);
            stopwatch.Stop();

            var statusCode = context.Response.StatusCode;
            var level = statusCode >= StatusCodes.Status500InternalServerError
                ? LogLevel.Error
                : statusCode >= StatusCodes.Status400BadRequest
                    ? LogLevel.Warning
                    : LogLevel.Information;

            logger.Log(
                level,
                "HTTP request completed. Method: {Method}. Path: {Path}. StatusCode: {StatusCode}. ElapsedMs: {ElapsedMs}. CorrelationId: {CorrelationId}. TraceId: {TraceId}.",
                context.Request.Method,
                context.Request.Path,
                statusCode,
                stopwatch.ElapsedMilliseconds,
                correlationId,
                context.TraceIdentifier);
        }
        catch (Exception exception)
        {
            stopwatch.Stop();

            logger.LogError(
                exception,
                "HTTP request failed. Method: {Method}. Path: {Path}. ElapsedMs: {ElapsedMs}. CorrelationId: {CorrelationId}. TraceId: {TraceId}.",
                context.Request.Method,
                context.Request.Path,
                stopwatch.ElapsedMilliseconds,
                correlationId,
                context.TraceIdentifier);

            throw;
        }
    }

    private static string ResolveCorrelationId(HttpContext context)
    {
        var value = context.Request.Headers[CorrelationId.HeaderName].FirstOrDefault();

        return string.IsNullOrWhiteSpace(value)
            ? context.TraceIdentifier
            : value.Trim();
    }
}

internal static class RequestLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app)
    {
        return app.UseMiddleware<RequestLoggingMiddleware>();
    }
}
