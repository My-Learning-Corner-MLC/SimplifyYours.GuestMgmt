using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GuestManagementService.Application.Common.Logging;

public sealed class RequestLoggingBehavior<TRequest, TResponse>(
    ILogger<RequestLoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();

        logger.LogInformation("Application request started. Request: {RequestName}.", requestName);

        try
        {
            var response = await next();
            stopwatch.Stop();

            logger.LogInformation(
                "Application request completed. Request: {RequestName}. ElapsedMs: {ElapsedMs}.",
                requestName,
                stopwatch.ElapsedMilliseconds);

            return response;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();
            logger.LogWarning(
                "Application request cancelled. Request: {RequestName}. ElapsedMs: {ElapsedMs}.",
                requestName,
                stopwatch.ElapsedMilliseconds);
            throw;
        }
        catch (Exception exception)
        {
            stopwatch.Stop();
            logger.LogError(
                exception,
                "Application request failed. Request: {RequestName}. ElapsedMs: {ElapsedMs}.",
                requestName,
                stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
