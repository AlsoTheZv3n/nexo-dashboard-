using Dashboard.Core.Abstractions;
using Dashboard.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dashboard.Infrastructure.Persistence.Repositories;

public sealed class AlertRuleRepository(DashboardDbContext db) : IAlertRuleRepository
{
    public async Task<IReadOnlyList<AlertRule>> GetAllAsync(CancellationToken ct = default) =>
        await db.AlertRules.AsNoTracking().OrderBy(r => r.Name).ToListAsync(ct);

    public async Task<IReadOnlyList<AlertRule>> GetActiveAsync(CancellationToken ct = default) =>
        await db.AlertRules.Where(r => r.IsActive).ToListAsync(ct);

    public Task<AlertRule?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.AlertRules.SingleOrDefaultAsync(r => r.Id == id, ct);

    public async Task AddAsync(AlertRule rule, CancellationToken ct = default)
    {
        db.AlertRules.Add(rule);
        await db.SaveChangesAsync(ct);
    }

    public Task UpdateAsync(AlertRule rule, CancellationToken ct = default)
    {
        db.AlertRules.Update(rule);
        return db.SaveChangesAsync(ct);
    }

    public Task RemoveAsync(AlertRule rule, CancellationToken ct = default)
    {
        db.AlertRules.Remove(rule);
        return db.SaveChangesAsync(ct);
    }
}

public sealed class AlertIncidentRepository(DashboardDbContext db) : IAlertIncidentRepository
{
    public async Task<IReadOnlyList<AlertIncident>> GetRecentAsync(int limit, CancellationToken ct = default) =>
        await db.AlertIncidents.AsNoTracking()
            .OrderByDescending(i => i.TriggeredAt)
            .Take(limit)
            .ToListAsync(ct);

    public Task<AlertIncident?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.AlertIncidents.SingleOrDefaultAsync(i => i.Id == id, ct);

    public Task<AlertIncident?> FindOpenForRuleAsync(Guid ruleId, CancellationToken ct = default) =>
        db.AlertIncidents
            .Where(i => i.RuleId == ruleId && i.State != AlertIncidentState.Resolved)
            .OrderByDescending(i => i.TriggeredAt)
            .FirstOrDefaultAsync(ct);

    public async Task AddAsync(AlertIncident incident, CancellationToken ct = default)
    {
        db.AlertIncidents.Add(incident);
        await db.SaveChangesAsync(ct);
    }

    public Task UpdateAsync(AlertIncident incident, CancellationToken ct = default)
    {
        db.AlertIncidents.Update(incident);
        return db.SaveChangesAsync(ct);
    }
}
