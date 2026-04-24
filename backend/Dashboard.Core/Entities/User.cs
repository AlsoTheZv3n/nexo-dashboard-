namespace Dashboard.Core.Entities;

public sealed class User
{
    public Guid Id { get; private set; }
    public string Username { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public UserRole Role { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? LastLoginAt { get; private set; }

    private User() { }

    public User(string username, string passwordHash, UserRole role)
    {
        Id = Guid.NewGuid();
        Username = username;
        PasswordHash = passwordHash;
        Role = role;
        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public void RecordLogin(DateTimeOffset when) => LastLoginAt = when;

    public void Deactivate() => IsActive = false;

    public void ChangePassword(string newHash)
    {
        if (string.IsNullOrWhiteSpace(newHash))
            throw new ArgumentException("Password hash cannot be empty.", nameof(newHash));
        PasswordHash = newHash;
    }
}

public enum UserRole
{
    Viewer = 0,
    Operator = 1,
    Admin = 2
}
