using Dashboard.Core.Abstractions;
using Dashboard.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dashboard.Infrastructure.Persistence.Repositories;

public sealed class ScriptRepository(DashboardDbContext db) : IScriptRepository
{
    public async Task<IReadOnlyList<PsScript>> GetAllAsync(CancellationToken ct = default) =>
        await db.Scripts.AsNoTracking().OrderBy(s => s.Name).ToListAsync(ct);

    public Task<PsScript?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Scripts.SingleOrDefaultAsync(s => s.Id == id, ct);

    public Task<PsScript?> GetByNameAsync(string name, CancellationToken ct = default) =>
        db.Scripts.SingleOrDefaultAsync(s => s.Name == name, ct);

    public async Task AddAsync(PsScript script, CancellationToken ct = default)
    {
        db.Scripts.Add(script);
        await db.SaveChangesAsync(ct);
    }

    public Task UpdateAsync(PsScript script, CancellationToken ct = default)
    {
        db.Scripts.Update(script);
        return db.SaveChangesAsync(ct);
    }
}
