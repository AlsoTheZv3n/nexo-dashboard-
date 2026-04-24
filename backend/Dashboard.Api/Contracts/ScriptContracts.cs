namespace Dashboard.Api.Contracts;

public sealed record ScriptDto(
    Guid Id,
    string Name,
    string Description,
    string FilePath,
    string MetaJson,
    DateTimeOffset UpdatedAt);
