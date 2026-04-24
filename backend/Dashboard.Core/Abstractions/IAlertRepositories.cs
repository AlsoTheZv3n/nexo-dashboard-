using Dashboard.Core.Entities;

namespace Dashboard.Core.Abstractions;

public interface IAlertRuleRepository
{
    Task<IReadOnlyList<AlertRule>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<AlertRule>> GetActiveAsync(CancellationToken ct = default);
    Task<AlertRule?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(AlertRule rule, CancellationToken ct = default);
    Task UpdateAsync(AlertRule rule, CancellationToken ct = default);
    Task RemoveAsync(AlertRule rule, CancellationToken ct = default);
}

public interface IAlertIncidentRepository
{
    Task<IReadOnlyList<AlertIncident>> GetRecentAsync(int limit, CancellationToken ct = default);
    Task<AlertIncident?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<AlertIncident?> FindOpenForRuleAsync(Guid ruleId, CancellationToken ct = default);
    Task AddAsync(AlertIncident incident, CancellationToken ct = default);
    Task UpdateAsync(AlertIncident incident, CancellationToken ct = default);
}
