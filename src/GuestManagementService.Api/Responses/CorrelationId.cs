namespace GuestManagementService.Api.Responses;

internal static class CorrelationId
{
    public const string HeaderName = "X-Correlation-Id";

    public static string Get(HttpContext context)
    {
        if (context.Items.TryGetValue(HeaderName, out var value)
            && value is string correlationId
            && !string.IsNullOrWhiteSpace(correlationId))
        {
            return correlationId;
        }

        return context.TraceIdentifier;
    }
}
