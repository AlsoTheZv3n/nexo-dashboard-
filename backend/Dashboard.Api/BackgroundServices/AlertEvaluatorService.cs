using System.Net.Http.Json;
using Dashboard.Core.Abstractions;
using Dashboard.Core.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace Dashboard.Api.BackgroundServices;

/// <summary>
/// Runs every minute: for each active AlertRule it aggregates the matching metric
/// in the configured sliding window and fires an incident when the threshold is
/// breached and no open incident already exists. Resolves the open incident
/// automatically when the metric recovers.
/// </summary>
public sealed class AlertEvaluatorService(
    IServiceScopeFactory scopes,
    IHttpClientFactory httpClients,
    ILogger<AlertEvaluatorService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Offset initial tick so we don't race startup migrations.
        try { await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken); } catch (OperationCanceledException) { return; }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await EvaluateOnceAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "AlertEvaluator tick failed");
            }

            try { await Task.Delay(Interval, stoppingToken); } catch (OperationCanceledException) { return; }
        }
    }

    private async Task EvaluateOnceAsync(CancellationToken ct)
    {
        using var scope = scopes.CreateScope();
        var rules = scope.ServiceProvider.GetRequiredService<IAlertRuleRepository>();
        var incidents = scope.ServiceProvider.GetRequiredService<IAlertIncidentRepository>();
        var metrics = scope.ServiceProvider.GetRequiredService<IMetricsRepository>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();

        var active = await rules.GetActiveAsync(ct);
        foreach (var rule in active)
        {
            var now = clock.UtcNow;
            var from = now.AddMinutes(-rule.WindowMinutes);

            var points = await metrics.GetTimeSeriesAsync(
                rule.MetricKey, from, now,
                MetricBucket.Hour,
                MapAggregation(rule.Aggregation),
                ct);
            rule.RecordEvaluation(now);
            await rules.UpdateAsync(rule, ct);

            var observed = points.Count == 0 ? 0 : points.Last().Value;
            var open = await incidents.FindOpenForRuleAsync(rule.Id, ct);

            if (rule.Evaluate(observed))
            {
                if (open is null)
                {
                    var incident = new AlertIncident(rule.Id, observed);
                    await incidents.AddAsync(incident, ct);
                    await NotifyAsync(rule, incident, observed, ct);
                }
            }
            else if (open is not null)
            {
                open.Resolve(now);
                await incidents.UpdateAsync(open, ct);
            }
        }
    }

    private async Task NotifyAsync(AlertRule rule, AlertIncident incident, double observed, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(rule.WebhookUrl)) return;

        try
        {
            var client = httpClients.CreateClient();
            using var response = await client.PostAsJsonAsync(rule.WebhookUrl, new
            {
                incidentId = incident.Id,
                ruleId = rule.Id,
                ruleName = rule.Name,
                metricKey = rule.MetricKey,
                @operator = rule.Operator.ToString(),
                threshold = rule.Threshold,
                observed,
                triggeredAt = incident.TriggeredAt,
            }, ct);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Alert webhook failed for rule {RuleId}", rule.Id);
        }
    }

    private static MetricAggregation MapAggregation(AlertAggregation a) => a switch
    {
        AlertAggregation.Avg => MetricAggregation.Avg,
        AlertAggregation.Sum => MetricAggregation.Sum,
        AlertAggregation.Min => MetricAggregation.Min,
        AlertAggregation.Max => MetricAggregation.Max,
        AlertAggregation.Count => MetricAggregation.Count,
        _ => MetricAggregation.Avg,
    };
}
