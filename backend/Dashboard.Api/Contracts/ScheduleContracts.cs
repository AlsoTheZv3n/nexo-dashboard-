using FluentValidation;
using NCrontab;

namespace Dashboard.Api.Contracts;

public sealed record ScheduleDto(
    Guid id,
    Guid scriptId,
    string name,
    string cronExpression,
    string parametersJson,
    bool isActive,
    DateTimeOffset? lastRunAt,
    DateTimeOffset? nextRunAt);

public sealed record CreateScheduleRequest(
    Guid ScriptId,
    string Name,
    string CronExpression,
    Dictionary<string, object>? Parameters);

public sealed record ToggleScheduleRequest(bool IsActive);

public sealed class CreateScheduleRequestValidator : AbstractValidator<CreateScheduleRequest>
{
    public CreateScheduleRequestValidator()
    {
        RuleFor(x => x.ScriptId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
        RuleFor(x => x.CronExpression).NotEmpty().Must(BeValidCron)
            .WithMessage("Invalid cron expression (expects 5 fields: minute hour day month dow).");
    }

    private static bool BeValidCron(string expr) =>
        CrontabSchedule.TryParse(expr) is not null;
}
