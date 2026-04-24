using Dashboard.Core.Abstractions;
using Dashboard.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dashboard.Infrastructure.Persistence.Repositories;

public sealed class AuditLogRepository(DashboardDbContext db) : IAuditLogRepository
{
    public async Task<(IReadOnlyList<AuditLogEntry> Items, int Total)> GetPagedAsync(
        int page,
        int pageSize,
        Guid? userId = null,
        string? action = null,
        DateTimeOffset? from = null,
        DateTimeOffset? to = null,
        CancellationToken ct = default)
    {
        var query = db.AuditLog.AsNoTracking().AsQueryable();
        if (userId.HasValue) query = query.Where(e => e.UserId == userId.Value);
        if (!string.IsNullOrWhiteSpace(action)) query = query.Where(e => e.Action == action);
        if (from.HasValue) query = query.Where(e => e.Timestamp >= from.Value);
        if (to.HasValue) query = query.Where(e => e.Timestamp < to.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(e => e.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
        return (items, total);
    }

    public async Task AddAsync(AuditLogEntry entry, CancellationToken ct = default)
    {
        db.AuditLog.Add(entry);
        await db.SaveChangesAsync(ct);
    }
}
