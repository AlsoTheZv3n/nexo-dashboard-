namespace Dashboard.Core.Entities;

public sealed class AuditLogEntry
{
    public Guid Id { get; private set; }
    public Guid? UserId { get; private set; }
    public string Action { get; private set; } = null!;
    public string? TargetType { get; private set; }
    public string? TargetId { get; private set; }
    public string? DetailsJson { get; private set; }
    public string? IpAddress { get; private set; }
    public DateTimeOffset Timestamp { get; private set; }

    private AuditLogEntry() { }

    public AuditLogEntry(
        Guid? userId,
        string action,
        string? targetType = null,
        string? targetId = null,
        string? detailsJson = null,
        string? ipAddress = null)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Action = action;
        TargetType = targetType;
        TargetId = targetId;
        DetailsJson = detailsJson;
        IpAddress = ipAddress;
        Timestamp = DateTimeOffset.UtcNow;
    }
}
