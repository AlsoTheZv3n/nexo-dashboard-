using Dashboard.Core.Entities;

namespace Dashboard.Core.Abstractions;

public enum MetricBucket
{
    Minute,
    Hour,
    Day,
    Week,
}

public enum MetricAggregation
{
    Avg,
    Sum,
    Min,
    Max,
    Count,
}

public sealed record MetricPoint(DateTimeOffset BucketStart, double Value, int Samples);

public sealed record ExecutionStatusBreakdown(ExecutionStatus Status, long Count);

public sealed record ScriptUsageRow(Guid ScriptId, string Name, long ExecutionCount);

public sealed record DashboardSummary(
    long ScriptCount,
    long ExecutionsLast24h,
    long FailuresLast24h,
    double AverageDurationSeconds);

public interface IMetricsRepository
{
    Task AddAsync(Metric metric, CancellationToken ct = default);

    Task AddManyAsync(IEnumerable<Metric> metrics, CancellationToken ct = default);

    Task<IReadOnlyList<MetricPoint>> GetTimeSeriesAsync(
        string key,
        DateTimeOffset from,
        DateTimeOffset to,
        MetricBucket bucket,
        MetricAggregation aggregation,
        CancellationToken ct = default);

    Task<IReadOnlyList<string>> GetKeysAsync(CancellationToken ct = default);

    Task<DashboardSummary> GetDashboardSummaryAsync(DateTimeOffset now, CancellationToken ct = default);

    Task<IReadOnlyList<ExecutionStatusBreakdown>> GetStatusBreakdownAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct = default);

    Task<IReadOnlyList<ScriptUsageRow>> GetTopScriptsAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        int limit,
        CancellationToken ct = default);
}
