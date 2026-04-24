using Dashboard.Core.Abstractions;
using Dashboard.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dashboard.Infrastructure.Persistence.Repositories;

public sealed class ApiKeyRepository(DashboardDbContext db) : IApiKeyRepository
{
    public async Task<IReadOnlyList<ApiKey>> GetAllAsync(CancellationToken ct = default) =>
        await db.ApiKeys.AsNoTracking().OrderByDescending(k => k.CreatedAt).ToListAsync(ct);

    public Task<ApiKey?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.ApiKeys.SingleOrDefaultAsync(k => k.Id == id, ct);

    public Task<ApiKey?> FindByHashAsync(string keyHash, CancellationToken ct = default) =>
        db.ApiKeys.SingleOrDefaultAsync(k => k.KeyHash == keyHash, ct);

    public async Task AddAsync(ApiKey apiKey, CancellationToken ct = default)
    {
        db.ApiKeys.Add(apiKey);
        await db.SaveChangesAsync(ct);
    }

    public Task UpdateAsync(ApiKey apiKey, CancellationToken ct = default)
    {
        db.ApiKeys.Update(apiKey);
        return db.SaveChangesAsync(ct);
    }
}
