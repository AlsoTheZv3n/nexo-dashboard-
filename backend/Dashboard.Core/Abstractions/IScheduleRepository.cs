using Dashboard.Core.Entities;

namespace Dashboard.Core.Abstractions;

public interface IScheduleRepository
{
    Task<IReadOnlyList<ScheduledExecution>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ScheduledExecution>> GetDueAsync(DateTimeOffset now, CancellationToken ct = default);
    Task<ScheduledExecution?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(ScheduledExecution schedule, CancellationToken ct = default);
    Task UpdateAsync(ScheduledExecution schedule, CancellationToken ct = default);
    Task RemoveAsync(ScheduledExecution schedule, CancellationToken ct = default);
}
