namespace Dashboard.Core.Entities;

public sealed class PsExecution
{
    public Guid Id { get; private set; }
    public Guid ScriptId { get; private set; }
    public Guid UserId { get; private set; }
    public ExecutionStatus Status { get; private set; }
    public string ParametersJson { get; private set; } = "{}";
    public string? Stdout { get; private set; }
    public string? Stderr { get; private set; }
    public int? ExitCode { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    private PsExecution() { }

    public PsExecution(Guid scriptId, Guid userId, string parametersJson)
    {
        Id = Guid.NewGuid();
        ScriptId = scriptId;
        UserId = userId;
        ParametersJson = parametersJson;
        Status = ExecutionStatus.Pending;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkRunning()
    {
        Status = ExecutionStatus.Running;
        StartedAt = DateTimeOffset.UtcNow;
    }

    public void MarkCompleted(string? stdout, string? stderr, int exitCode)
    {
        Status = exitCode == 0 ? ExecutionStatus.Completed : ExecutionStatus.Failed;
        Stdout = stdout;
        Stderr = stderr;
        ExitCode = exitCode;
        CompletedAt = DateTimeOffset.UtcNow;
    }

    public void MarkCancelled()
    {
        Status = ExecutionStatus.Cancelled;
        CompletedAt = DateTimeOffset.UtcNow;
    }

    public void MarkFailed(string reason)
    {
        Status = ExecutionStatus.Failed;
        Stderr = reason;
        CompletedAt = DateTimeOffset.UtcNow;
    }
}

public enum ExecutionStatus
{
    Pending = 0,
    Running = 1,
    Completed = 2,
    Failed = 3,
    Cancelled = 4
}
