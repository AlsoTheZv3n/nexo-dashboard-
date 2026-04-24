namespace Dashboard.Api.Contracts;

public sealed record AuditLogEntryDto(
    Guid id,
    Guid? userId,
    string action,
    string? targetType,
    string? targetId,
    string? detailsJson,
    string? ipAddress,
    DateTimeOffset timestamp);
