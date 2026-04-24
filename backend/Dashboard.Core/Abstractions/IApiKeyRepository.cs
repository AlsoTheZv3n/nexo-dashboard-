using Dashboard.Core.Entities;

namespace Dashboard.Core.Abstractions;

public interface IApiKeyRepository
{
    Task<IReadOnlyList<ApiKey>> GetAllAsync(CancellationToken ct = default);
    Task<ApiKey?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<ApiKey?> FindByHashAsync(string keyHash, CancellationToken ct = default);
    Task AddAsync(ApiKey apiKey, CancellationToken ct = default);
    Task UpdateAsync(ApiKey apiKey, CancellationToken ct = default);
}
