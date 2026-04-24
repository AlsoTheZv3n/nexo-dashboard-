using Dashboard.Core.Abstractions;
using Dashboard.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dashboard.Infrastructure.Persistence.Repositories;

public sealed class ExecutionRepository(DashboardDbContext db) : IExecutionRepository
{
    public Task<PsExecution?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Executions.SingleOrDefaultAsync(e => e.Id == id, ct);

    public async Task<(IReadOnlyList<PsExecution> Items, int Total)> GetPagedAsync(
        int page,
        int pageSize,
        Guid? scriptId = null,
        ExecutionStatus? status = null,
        CancellationToken ct = default)
    {
        var query = db.Executions.AsNoTracking().AsQueryable();
        if (scriptId.HasValue) query = query.Where(e => e.ScriptId == scriptId.Value);
        if (status.HasValue) query = query.Where(e => e.Status == status.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(e => e.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task AddAsync(PsExecution execution, CancellationToken ct = default)
    {
        db.Executions.Add(execution);
        await db.SaveChangesAsync(ct);
    }

    public Task UpdateAsync(PsExecution execution, CancellationToken ct = default)
    {
        db.Executions.Update(execution);
        return db.SaveChangesAsync(ct);
    }
}
