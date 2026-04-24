using FluentValidation;

namespace Dashboard.Api.Contracts;

public sealed record CreateMetricRequest(
    string Key,
    double Value,
    DateTimeOffset? Timestamp,
    Dictionary<string, string>? Tags);

public sealed record CreateMetricsBulkRequest(IReadOnlyList<CreateMetricRequest> Items);

public sealed record MetricPointDto(DateTimeOffset bucketStart, double value, int samples);

public sealed record TimeseriesResponse(
    string key,
    string bucket,
    string aggregation,
    DateTimeOffset from,
    DateTimeOffset to,
    IReadOnlyList<MetricPointDto> points);

public sealed record SummaryResponse(
    long scriptCount,
    long executionsLast24h,
    long failuresLast24h,
    double averageDurationSeconds);

public sealed record StatusBreakdownRow(string status, long count);

public sealed record TopScriptRow(Guid scriptId, string name, long executions);

public sealed class CreateMetricRequestValidator : AbstractValidator<CreateMetricRequest>
{
    public CreateMetricRequestValidator()
    {
        RuleFor(x => x.Key).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Value).Must(v => !double.IsNaN(v) && !double.IsInfinity(v))
            .WithMessage("Value must be a finite number.");
    }
}

public sealed class CreateMetricsBulkRequestValidator : AbstractValidator<CreateMetricsBulkRequest>
{
    public CreateMetricsBulkRequestValidator()
    {
        RuleFor(x => x.Items).NotEmpty().Must(i => i.Count <= 1000)
            .WithMessage("At most 1000 metrics per request.");
        RuleForEach(x => x.Items).SetValidator(new CreateMetricRequestValidator());
    }
}
