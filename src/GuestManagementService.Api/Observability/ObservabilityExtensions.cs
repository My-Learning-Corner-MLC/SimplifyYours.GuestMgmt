using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace GuestManagementService.Api.Observability;

internal static class ObservabilityExtensions
{
    private const string ServiceNamespace = "SimplifyYours";

    public static WebApplicationBuilder AddServiceObservability(
        this WebApplicationBuilder builder,
        string serviceName)
    {
        var resourceBuilder = CreateResourceBuilder(builder, serviceName);

        builder.Logging.AddOpenTelemetry(options =>
        {
            options.IncludeFormattedMessage = true;
            options.IncludeScopes = true;
            options.ParseStateValues = true;
            options.SetResourceBuilder(resourceBuilder);
            options.AddOtlpExporter();
        });

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => ConfigureResource(resource, builder, serviceName))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation(options => options.RecordException = true)
                .AddHttpClientInstrumentation()
                .AddOtlpExporter())
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddOtlpExporter());

        return builder;
    }

    private static ResourceBuilder CreateResourceBuilder(
        WebApplicationBuilder builder,
        string serviceName)
    {
        return ConfigureResource(ResourceBuilder.CreateDefault(), builder, serviceName);
    }

    private static ResourceBuilder ConfigureResource(
        ResourceBuilder resource,
        WebApplicationBuilder builder,
        string serviceName)
    {
        return resource
            .AddService(serviceName: serviceName, serviceNamespace: ServiceNamespace)
            .AddAttributes([
                new KeyValuePair<string, object>("deployment.environment", builder.Environment.EnvironmentName)
            ]);
    }
}
