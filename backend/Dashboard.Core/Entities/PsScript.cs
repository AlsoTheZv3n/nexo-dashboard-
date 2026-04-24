namespace Dashboard.Core.Entities;

public sealed class PsScript
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public string FilePath { get; private set; } = null!;
    public string MetaJson { get; private set; } = null!;
    public string ScriptHash { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private PsScript() { }

    public PsScript(string name, string description, string filePath, string metaJson, string scriptHash)
    {
        Id = Guid.NewGuid();
        Name = name;
        Description = description;
        FilePath = filePath;
        MetaJson = metaJson;
        ScriptHash = scriptHash;
        CreatedAt = UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Update(string description, string metaJson, string scriptHash)
    {
        Description = description;
        MetaJson = metaJson;
        ScriptHash = scriptHash;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
