using Dashboard.Core.Abstractions;
using Dashboard.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dashboard.Infrastructure.Persistence.Repositories;

public sealed class UserRepository(DashboardDbContext db) : IUserRepository
{
    public Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default) =>
        db.Users.SingleOrDefaultAsync(u => u.Username == username, ct);

    public Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Users.SingleOrDefaultAsync(u => u.Id == id, ct);

    public async Task AddAsync(User user, CancellationToken ct = default)
    {
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
    }

    public Task UpdateAsync(User user, CancellationToken ct = default)
    {
        db.Users.Update(user);
        return db.SaveChangesAsync(ct);
    }
}
