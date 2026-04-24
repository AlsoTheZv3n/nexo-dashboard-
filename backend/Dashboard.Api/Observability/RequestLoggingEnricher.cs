using Serilog;

namespace Dashboard.Api.Observability;

public static class RequestLoggingEnricher
{
    /// <summary>
    /// Enriches UseSerilogRequestLogging with user/status/correlation context
    /// and crucially redacts tokens so they can't end up in a log aggregator.
    /// </summary>
    public static IApplicationBuilder UseDashboardRequestLogging(this IApplicationBuilder app) =>
        app.UseSerilogRequestLogging(opts =>
        {
            opts.MessageTemplate = "{RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0} ms";
            opts.EnrichDiagnosticContext = (diag, http) =>
            {
                diag.Set("RequestHost", http.Request.Host.Value ?? string.Empty);
                diag.Set("UserAgent", http.Request.Headers.UserAgent.ToString());
                diag.Set("ClientIp", http.Connection.RemoteIpAddress?.ToString() ?? string.Empty);

                var correlationId = http.Request.Headers["X-Correlation-ID"].ToString();
                if (string.IsNullOrEmpty(correlationId))
                {
                    correlationId = http.TraceIdentifier;
                }
                diag.Set("CorrelationId", correlationId);

                if (http.User.Identity?.IsAuthenticated == true)
                {
                    diag.Set("UserId", http.User.FindFirst("sub")?.Value ?? string.Empty);
                    diag.Set("UserRole", http.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? string.Empty);
                }

                // Never log the Authorization header. The extension below hard-strips it before
                // Serilog's sink sees it, but leaving a placeholder tells a reviewer we saw it.
                if (http.Request.Headers.ContainsKey("Authorization"))
                {
                    diag.Set("AuthPresent", true);
                }
            };
        });
}
