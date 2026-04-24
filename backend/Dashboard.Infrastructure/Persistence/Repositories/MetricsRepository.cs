using Dashboard.Core.Abstractions;
using Dashboard.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dashboard.Infrastructure.Persistence.Repositories;

public sealed class MetricsRepository(DashboardDbContext db) : IMetricsRepository
{
    public async Task AddAsync(Metric metric, CancellationToken ct = default)
    {
        db.Metrics.Add(metric);
        await db.SaveChangesAsync(ct);
    }

    public async Task AddManyAsync(IEnumerable<Metric> metrics, CancellationToken ct = default)
    {
        db.Metrics.AddRange(metrics);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<string>> GetKeysAsync(CancellationToken ct = default) =>
        await db.Metrics.Select(m => m.Key).Distinct().OrderBy(k => k).ToListAsync(ct);

    public async Task<IReadOnlyList<MetricPoint>> GetTimeSeriesAsync(
        string key,
        DateTimeOffset from,
        DateTimeOffset to,
        MetricBucket bucket,
        MetricAggregation aggregation,
        CancellationToken ct = default)
    {
        // Fetch raw rows then bucket in memory. Portable across SQL dialects and fine for
        // typical time-windows; if row counts ever explode this is the obvious place to
        // swap in a date_trunc-based raw SQL query for Postgres.
        var rows = await db.Metrics.AsNoTracking()
            .Where(m => m.Key == key && m.Timestamp >= from && m.Timestamp < to)
            .Select(m => new { m.Timestamp, m.Value })
            .ToListAsync(ct);

        var grouped = rows
            .GroupBy(r => Truncate(r.Timestamp, bucket))
            .Select(g => new MetricPoint(
                g.Key,
                Aggregate(g.Select(x => x.Value), aggregation),
                g.Count()))
            .OrderBy(p => p.BucketStart)
            .ToList();

        return grouped;
    }

    public async Task<DashboardSummary> GetDashboardSummaryAsync(DateTimeOffset now, CancellationToken ct = default)
    {
        var cutoff = now.AddHours(-24);

        var scriptCount = await db.Scripts.CountAsync(ct);

        var recentExecs = await db.Executions.AsNoTracking()
            .Where(e => e.CreatedAt >= cutoff)
            .Select(e => new { e.Status, e.StartedAt, e.CompletedAt })
            .ToListAsync(ct);

        var total = recentExecs.Count;
        var failures = recentExecs.Count(e => e.Status == ExecutionStatus.Failed);

        var finishedWithTiming = recentExecs
            .Where(e => e.StartedAt.HasValue && e.CompletedAt.HasValue)
            .Select(e => (e.CompletedAt!.Value - e.StartedAt!.Value).TotalSeconds)
            .ToList();

        var avgSeconds = finishedWithTiming.Count == 0 ? 0 : finishedWithTiming.Average();

        return new DashboardSummary(scriptCount, total, failures, Math.Round(avgSeconds, 2));
    }

    public async Task<IReadOnlyList<ExecutionStatusBreakdown>> GetStatusBreakdownAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct = default)
    {
        var rows = await db.Executions.AsNoTracking()
            .Where(e => e.CreatedAt >= from && e.CreatedAt < to)
            .GroupBy(e => e.Status)
            .Select(g => new ExecutionStatusBreakdown(g.Key, g.LongCount()))
            .ToListAsync(ct);

        return rows;
    }

    public async Task<IReadOnlyList<ScriptUsageRow>> GetTopScriptsAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        int limit,
        CancellationToken ct = default)
    {
        var rangeFrom = from;
        var rangeTo = to;
        var query =
            from exec in db.Executions.AsNoTracking()
            where exec.CreatedAt >= rangeFrom && exec.CreatedAt < rangeTo
            join script in db.Scripts.AsNoTracking() on exec.ScriptId equals script.Id
            group new { exec, script } by new { exec.ScriptId, script.Name } into g
            orderby g.LongCount() descending
            select new ScriptUsageRow(g.Key.ScriptId, g.Key.Name, g.LongCount());

        return await query.Take(limit).ToListAsync(ct);
    }

    public static DateTimeOffset Truncate(DateTimeOffset ts, MetricBucket bucket)
    {
        var utc = ts.ToUniversalTime();
        return bucket switch
        {
            MetricBucket.Minute => new DateTimeOffset(utc.Year, utc.Month, utc.Day, utc.Hour, utc.Minute, 0, TimeSpan.Zero),
            MetricBucket.Hour => new DateTimeOffset(utc.Year, utc.Month, utc.Day, utc.Hour, 0, 0, TimeSpan.Zero),
            MetricBucket.Day => new DateTimeOffset(utc.Year, utc.Month, utc.Day, 0, 0, 0, TimeSpan.Zero),
            MetricBucket.Week => StartOfIsoWeek(utc),
            _ => utc,
        };
    }

    private static DateTimeOffset StartOfIsoWeek(DateTimeOffset ts)
    {
        var day = (int)ts.DayOfWeek;
        // ISO week starts on Monday; DayOfWeek.Sunday = 0 in .NET.
        var delta = day == 0 ? -6 : 1 - day;
        var monday = ts.Date.AddDays(delta);
        return new DateTimeOffset(monday, TimeSpan.Zero);
    }

    public static double Aggregate(IEnumerable<double> values, MetricAggregation aggregation)
    {
        var list = values.ToList();
        if (list.Count == 0) return 0;
        return aggregation switch
        {
            MetricAggregation.Avg => list.Average(),
            MetricAggregation.Sum => list.Sum(),
            MetricAggregation.Min => list.Min(),
            MetricAggregation.Max => list.Max(),
            MetricAggregation.Count => list.Count,
            _ => list.Average(),
        };
    }
}
