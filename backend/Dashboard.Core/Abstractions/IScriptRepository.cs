using Dashboard.Core.Entities;

namespace Dashboard.Core.Abstractions;

public interface IScriptRepository
{
    Task<IReadOnlyList<PsScript>> GetAllAsync(CancellationToken ct = default);
    Task<PsScript?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PsScript?> GetByNameAsync(string name, CancellationToken ct = default);
    Task AddAsync(PsScript script, CancellationToken ct = default);
    Task UpdateAsync(PsScript script, CancellationToken ct = default);
}
