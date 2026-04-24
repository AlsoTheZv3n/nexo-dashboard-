using Dashboard.Core.Abstractions;
using Dashboard.Core.Entities;

namespace Dashboard.Core.Services;

public sealed class AuditService(IAuditLogRepository log)
{
    public Task RecordAsync(
        Guid? userId,
        string action,
        string? targetType = null,
        string? targetId = null,
        string? detailsJson = null,
        string? ipAddress = null,
        CancellationToken ct = default)
    {
        var entry = new AuditLogEntry(userId, action, targetType, targetId, detailsJson, ipAddress);
        return log.AddAsync(entry, ct);
    }
}
