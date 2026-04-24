namespace Dashboard.Core.Entities;

public sealed class ScheduledExecution
{
    public Guid Id { get; private set; }
    public Guid ScriptId { get; private set; }
    public string Name { get; private set; } = null!;
    public string CronExpression { get; private set; } = null!;
    public string ParametersJson { get; private set; } = "{}";
    public bool IsActive { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? LastRunAt { get; private set; }
    public DateTimeOffset? NextRunAt { get; private set; }

    private ScheduledExecution() { }

    public ScheduledExecution(
        Guid scriptId,
        string name,
        string cronExpression,
        string parametersJson,
        Guid createdBy,
        DateTimeOffset? firstNextRun = null)
    {
        Id = Guid.NewGuid();
        ScriptId = scriptId;
        Name = name;
        CronExpression = cronExpression;
        ParametersJson = parametersJson;
        IsActive = true;
        CreatedByUserId = createdBy;
        CreatedAt = DateTimeOffset.UtcNow;
        NextRunAt = firstNextRun;
    }

    public void SetActive(bool active) => IsActive = active;

    public void RecordRun(DateTimeOffset ranAt, DateTimeOffset? nextRunAt)
    {
        LastRunAt = ranAt;
        NextRunAt = nextRunAt;
    }
}
