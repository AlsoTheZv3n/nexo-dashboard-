namespace Dashboard.Core.Entities;

public sealed class Metric
{
    public Guid Id { get; private set; }
    public string Key { get; private set; } = null!;
    public double Value { get; private set; }
    public string? TagsJson { get; private set; }
    public DateTimeOffset Timestamp { get; private set; }

    private Metric() { }

    public Metric(string key, double value, DateTimeOffset timestamp, string? tagsJson = null)
    {
        Id = Guid.NewGuid();
        Key = key;
        Value = value;
        Timestamp = timestamp;
        TagsJson = tagsJson;
    }
}
