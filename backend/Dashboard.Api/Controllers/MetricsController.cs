using System.Text.Json;
using Dashboard.Api.Contracts;
using Dashboard.Core.Abstractions;
using Dashboard.Core.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dashboard.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/metrics")]
public sealed class MetricsController(IMetricsRepository metrics, IClock clock) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = "Admin,Operator")]
    public async Task<IActionResult> Create([FromBody] CreateMetricRequest req, CancellationToken ct)
    {
        var metric = new Metric(
            req.Key,
            req.Value,
            req.Timestamp ?? clock.UtcNow,
            req.Tags is { Count: > 0 } ? JsonSerializer.Serialize(req.Tags) : null);

        await metrics.AddAsync(metric, ct);
        return Created($"/api/v1/metrics?key={Uri.EscapeDataString(req.Key)}", null);
    }

    [HttpPost("bulk")]
    [Authorize(Roles = "Admin,Operator")]
    public async Task<IActionResult> CreateBulk([FromBody] CreateMetricsBulkRequest req, CancellationToken ct)
    {
        var now = clock.UtcNow;
        var batch = req.Items.Select(item => new Metric(
            item.Key,
            item.Value,
            item.Timestamp ?? now,
            item.Tags is { Count: > 0 } ? JsonSerializer.Serialize(item.Tags) : null)).ToList();

        await metrics.AddManyAsync(batch, ct);
        return Created($"/api/v1/metrics/bulk", new { accepted = batch.Count });
    }

    [HttpGet("keys")]
    public async Task<IActionResult> GetKeys(CancellationToken ct)
    {
        var keys = await metrics.GetKeysAsync(ct);
        return Ok(keys);
    }

    [HttpGet("timeseries")]
    public async Task<IActionResult> GetTimeseries(
        [FromQuery] string key,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] string bucket = "hour",
        [FromQuery] string aggregation = "avg",
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            return Problem(statusCode: 400, title: "Validation", detail: "`key` query parameter is required.");

        if (!TryParseBucket(bucket, out var b))
            return Problem(statusCode: 400, title: "Validation", detail: $"Unknown bucket '{bucket}'. Use minute|hour|day|week.");

        if (!TryParseAggregation(aggregation, out var agg))
            return Problem(statusCode: 400, title: "Validation", detail: $"Unknown aggregation '{aggregation}'. Use avg|sum|min|max|count.");

        var now = clock.UtcNow;
        var rangeTo = to ?? now;
        var rangeFrom = from ?? rangeTo.AddDays(-7);
        if (rangeFrom >= rangeTo)
            return Problem(statusCode: 400, title: "Validation", detail: "`from` must be before `to`.");

        var points = await metrics.GetTimeSeriesAsync(key, rangeFrom, rangeTo, b, agg, ct);

        return Ok(new TimeseriesResponse(
            key,
            b.ToString().ToLowerInvariant(),
            agg.ToString().ToLowerInvariant(),
            rangeFrom,
            rangeTo,
            points.Select(p => new MetricPointDto(p.BucketStart, p.Value, p.Samples)).ToList()));
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken ct)
    {
        var summary = await metrics.GetDashboardSummaryAsync(clock.UtcNow, ct);
        return Ok(new SummaryResponse(
            summary.ScriptCount,
            summary.ExecutionsLast24h,
            summary.FailuresLast24h,
            summary.AverageDurationSeconds));
    }

    [HttpGet("status-breakdown")]
    public async Task<IActionResult> GetStatusBreakdown(
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        CancellationToken ct = default)
    {
        var now = clock.UtcNow;
        var rangeTo = to ?? now;
        var rangeFrom = from ?? rangeTo.AddDays(-7);
        var rows = await metrics.GetStatusBreakdownAsync(rangeFrom, rangeTo, ct);
        return Ok(rows.Select(r => new StatusBreakdownRow(r.Status.ToString(), r.Count)));
    }

    [HttpGet("top-scripts")]
    public async Task<IActionResult> GetTopScripts(
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] int limit = 5,
        CancellationToken ct = default)
    {
        limit = Math.Clamp(limit, 1, 50);
        var now = clock.UtcNow;
        var rangeTo = to ?? now;
        var rangeFrom = from ?? rangeTo.AddDays(-7);
        var rows = await metrics.GetTopScriptsAsync(rangeFrom, rangeTo, limit, ct);
        return Ok(rows.Select(r => new TopScriptRow(r.ScriptId, r.Name, r.ExecutionCount)));
    }

    private static bool TryParseBucket(string s, out MetricBucket b) =>
        Enum.TryParse(s, ignoreCase: true, out b);

    private static bool TryParseAggregation(string s, out MetricAggregation a) =>
        Enum.TryParse(s, ignoreCase: true, out a);
}
