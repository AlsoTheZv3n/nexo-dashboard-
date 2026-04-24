using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Dashboard.Api.Observability;

public static class ObservabilityConfig
{
    /// <summary>
    /// Wires OpenTelemetry traces + metrics and exposes a Prometheus-formatted
    /// /metrics endpoint. Tracing + instrumentation auto-attach to ASP.NET,
    /// HttpClient, and EF Core; custom ActivitySources can be registered by
    /// appending to the builder returned by AddSource.
    /// </summary>
    public static WebApplicationBuilder AddDashboardObservability(this WebApplicationBuilder builder)
    {
        var serviceName = "dashboard-api";
        var serviceVersion = typeof(ObservabilityConfig).Assembly.GetName().Version?.ToString() ?? "0.1.0";

        var resource = ResourceBuilder.CreateDefault()
            .AddService(serviceName, serviceVersion: serviceVersion)
            .AddAttributes(new KeyValuePair<string, object>[]
            {
                new("deployment.environment", builder.Environment.EnvironmentName)
            });

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService(serviceName, serviceVersion: serviceVersion))
            .WithTracing(tracing => tracing
                .SetResourceBuilder(resource)
                .AddAspNetCoreInstrumentation(opts =>
                {
                    // Never trace the /metrics scrape — it would create an endless self-loop of spans.
                    opts.Filter = ctx => !ctx.Request.Path.StartsWithSegments("/metrics");
                })
                .AddHttpClientInstrumentation()
                .AddEntityFrameworkCoreInstrumentation())
            .WithMetrics(metrics => metrics
                .SetResourceBuilder(resource)
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddMeter("Dashboard.*")
                .AddPrometheusExporter());

        return builder;
    }

    public static IEndpointRouteBuilder MapDashboardMetricsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPrometheusScrapingEndpoint();
        return endpoints;
    }
}
