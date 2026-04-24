using Dashboard.Core.Abstractions;
using Dashboard.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dashboard.Infrastructure.Persistence.Repositories;

public sealed class ScheduleRepository(DashboardDbContext db) : IScheduleRepository
{
    public async Task<IReadOnlyList<ScheduledExecution>> GetAllAsync(CancellationToken ct = default) =>
        await db.ScheduledExecutions.AsNoTracking()
            .OrderBy(s => s.Name)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ScheduledExecution>> GetDueAsync(DateTimeOffset now, CancellationToken ct = default) =>
        await db.ScheduledExecutions
            .Where(s => s.IsActive && s.NextRunAt != null && s.NextRunAt <= now)
            .ToListAsync(ct);

    public Task<ScheduledExecution?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.ScheduledExecutions.SingleOrDefaultAsync(s => s.Id == id, ct);

    public async Task AddAsync(ScheduledExecution schedule, CancellationToken ct = default)
    {
        db.ScheduledExecutions.Add(schedule);
        await db.SaveChangesAsync(ct);
    }

    public Task UpdateAsync(ScheduledExecution schedule, CancellationToken ct = default)
    {
        db.ScheduledExecutions.Update(schedule);
        return db.SaveChangesAsync(ct);
    }

    public Task RemoveAsync(ScheduledExecution schedule, CancellationToken ct = default)
    {
        db.ScheduledExecutions.Remove(schedule);
        return db.SaveChangesAsync(ct);
    }
}
