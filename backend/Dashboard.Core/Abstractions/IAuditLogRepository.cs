using Dashboard.Core.Entities;

namespace Dashboard.Core.Abstractions;

public interface IAuditLogRepository
{
    Task<(IReadOnlyList<AuditLogEntry> Items, int Total)> GetPagedAsync(
        int page,
        int pageSize,
        Guid? userId = null,
        string? action = null,
        DateTimeOffset? from = null,
        DateTimeOffset? to = null,
        CancellationToken ct = default);

    Task AddAsync(AuditLogEntry entry, CancellationToken ct = default);
}
