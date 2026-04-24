using Dashboard.Core.Entities;

namespace Dashboard.Core.Abstractions;

public interface IUserRepository
{
    Task<IReadOnlyList<User>> GetAllAsync(CancellationToken ct = default);
    Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default);
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(User user, CancellationToken ct = default);
    Task UpdateAsync(User user, CancellationToken ct = default);
}
