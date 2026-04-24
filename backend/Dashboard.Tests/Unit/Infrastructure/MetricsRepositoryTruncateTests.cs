using Dashboard.Core.Abstractions;
using Dashboard.Infrastructure.Persistence.Repositories;

namespace Dashboard.Tests.Unit.Infrastructure;

public class MetricsRepositoryTruncateTests
{
    [Theory]
    [InlineData(MetricBucket.Minute, 2026, 4, 24, 14, 37, 42, 2026, 4, 24, 14, 37, 0)]
    [InlineData(MetricBucket.Hour, 2026, 4, 24, 14, 37, 42, 2026, 4, 24, 14, 0, 0)]
    [InlineData(MetricBucket.Day, 2026, 4, 24, 14, 37, 42, 2026, 4, 24, 0, 0, 0)]
    public void Truncate_RoundsDownToBucket(MetricBucket bucket,
        int y, int m, int d, int h, int mi, int s,
        int ey, int em, int ed, int eh, int emi, int es)
    {
        var ts = new DateTimeOffset(y, m, d, h, mi, s, TimeSpan.Zero);
        var expected = new DateTimeOffset(ey, em, ed, eh, emi, es, TimeSpan.Zero);
        MetricsRepository.Truncate(ts, bucket).Should().Be(expected);
    }

    [Fact]
    public void Truncate_Week_ReturnsMonday()
    {
        // 2026-04-24 is a Friday (per real calendar); Monday of that ISO week = 2026-04-20.
        var ts = new DateTimeOffset(2026, 4, 24, 14, 30, 0, TimeSpan.Zero);
        var monday = MetricsRepository.Truncate(ts, MetricBucket.Week);
        monday.DayOfWeek.Should().Be(DayOfWeek.Monday);
        monday.Should().BeBefore(ts);
    }

    [Theory]
    [InlineData(MetricAggregation.Sum, 10)]
    [InlineData(MetricAggregation.Avg, 2.5)]
    [InlineData(MetricAggregation.Min, 1)]
    [InlineData(MetricAggregation.Max, 4)]
    [InlineData(MetricAggregation.Count, 4)]
    public void Aggregate_ComputesCorrectly(MetricAggregation agg, double expected)
    {
        var values = new[] { 1.0, 2.0, 3.0, 4.0 };
        MetricsRepository.Aggregate(values, agg).Should().Be(expected);
    }

    [Fact]
    public void Aggregate_EmptyInput_ReturnsZero()
    {
        MetricsRepository.Aggregate(Array.Empty<double>(), MetricAggregation.Avg).Should().Be(0);
    }
}
