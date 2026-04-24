namespace Dashboard.Core.Entities;

public enum AlertOperator
{
    GreaterThan = 0,
    LessThan = 1,
    Equals = 2,
}

public enum AlertAggregation
{
    Avg = 0,
    Sum = 1,
    Min = 2,
    Max = 3,
    Count = 4,
}

public sealed class AlertRule
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string MetricKey { get; private set; } = null!;
    public AlertOperator Operator { get; private set; }
    public double Threshold { get; private set; }
    public int WindowMinutes { get; private set; }
    public AlertAggregation Aggregation { get; private set; }
    public string? WebhookUrl { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? LastEvaluatedAt { get; private set; }

    private AlertRule() { }

    public AlertRule(
        string name,
        string metricKey,
        AlertOperator op,
        double threshold,
        int windowMinutes,
        AlertAggregation aggregation,
        string? webhookUrl)
    {
        Id = Guid.NewGuid();
        Name = name;
        MetricKey = metricKey;
        Operator = op;
        Threshold = threshold;
        WindowMinutes = windowMinutes;
        Aggregation = aggregation;
        WebhookUrl = webhookUrl;
        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public void RecordEvaluation(DateTimeOffset when) => LastEvaluatedAt = when;
    public void SetActive(bool active) => IsActive = active;

    public bool Evaluate(double observed) => Operator switch
    {
        AlertOperator.GreaterThan => observed > Threshold,
        AlertOperator.LessThan => observed < Threshold,
        AlertOperator.Equals => Math.Abs(observed - Threshold) < 0.0001,
        _ => false,
    };
}
