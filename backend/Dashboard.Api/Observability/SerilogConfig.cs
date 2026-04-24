using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace Dashboard.Api.Observability;

public static class SerilogConfig {
    /// <summary>
    /// Wires Serilog so Dev gets a readable console and Prod/Staging get
    /// CLEF (compact JSON) for log aggregators like Loki/Elastic.
    /// </summary>
    public static void Configure(this IHostBuilder host)
    {
        host.UseSerilog((ctx, cfg) =>
        {
            cfg.ReadFrom.Configuration(ctx.Configuration)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithProperty("service", "dashboard-api")
                .Enrich.WithProperty("environment", ctx.HostingEnvironment.EnvironmentName)
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning);

            if (ctx.HostingEnvironment.IsDevelopment())
            {
                cfg.WriteTo.Console();
            }
            else
            {
                cfg.WriteTo.Console(new CompactJsonFormatter());
            }
        });
    }
}
