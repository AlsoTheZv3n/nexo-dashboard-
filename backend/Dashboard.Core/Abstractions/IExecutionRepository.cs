using Dashboard.Core.Entities;

namespace Dashboard.Core.Abstractions;

public interface IExecutionRepository
{
    Task<PsExecution?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<(IReadOnlyList<PsExecution> Items, int Total)> GetPagedAsync(
        int page,
        int pageSize,
        Guid? scriptId = null,
        ExecutionStatus? status = null,
        CancellationToken ct = default);
    Task AddAsync(PsExecution execution, CancellationToken ct = default);
    Task UpdateAsync(PsExecution execution, CancellationToken ct = default);
}
