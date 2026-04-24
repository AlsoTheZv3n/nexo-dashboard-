namespace Dashboard.Core.Entities;

public sealed class ApiKey
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string KeyHash { get; private set; } = null!;
    public string Prefix { get; private set; } = null!;
    public Guid CreatedByUserId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? LastUsedAt { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }
    public bool IsActive { get; private set; }
    public UserRole Role { get; private set; }

    private ApiKey() { }

    public ApiKey(string name, string keyHash, string prefix, Guid createdBy, UserRole role, DateTimeOffset? expiresAt = null)
    {
        Id = Guid.NewGuid();
        Name = name;
        KeyHash = keyHash;
        Prefix = prefix;
        CreatedByUserId = createdBy;
        CreatedAt = DateTimeOffset.UtcNow;
        ExpiresAt = expiresAt;
        IsActive = true;
        Role = role;
    }

    public void RecordUse(DateTimeOffset when) => LastUsedAt = when;
    public void Revoke() => IsActive = false;

    public bool IsUsable(DateTimeOffset now) =>
        IsActive && (ExpiresAt is null || ExpiresAt > now);
}
