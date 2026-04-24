using FluentValidation;

namespace Dashboard.Api.Contracts;

public sealed record CreateExecutionRequest(Guid ScriptId, Dictionary<string, object>? Parameters);

public sealed record ExecutionDto(
    Guid Id,
    Guid ScriptId,
    string Status,
    string? Stdout,
    string? Stderr,
    int? ExitCode,
    DateTimeOffset CreatedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt);

public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int Total);

public sealed class CreateExecutionRequestValidator : AbstractValidator<CreateExecutionRequest>
{
    public CreateExecutionRequestValidator()
    {
        RuleFor(x => x.ScriptId).NotEmpty();
    }
}
