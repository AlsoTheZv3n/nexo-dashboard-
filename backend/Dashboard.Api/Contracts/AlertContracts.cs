using Dashboard.Core.Entities;
using FluentValidation;

namespace Dashboard.Api.Contracts;

public sealed record AlertRuleDto(
    Guid id,
    string name,
    string metricKey,
    string @operator,
    double threshold,
    int windowMinutes,
    string aggregation,
    string? webhookUrl,
    bool isActive,
    DateTimeOffset? lastEvaluatedAt);

public sealed record CreateAlertRuleRequest(
    string Name,
    string MetricKey,
    AlertOperator Operator,
    double Threshold,
    int WindowMinutes,
    AlertAggregation Aggregation,
    string? WebhookUrl);

public sealed record AlertIncidentDto(
    Guid id,
    Guid ruleId,
    string state,
    double observedValue,
    DateTimeOffset triggeredAt,
    DateTimeOffset? acknowledgedAt,
    Guid? acknowledgedByUserId);

public sealed class CreateAlertRuleRequestValidator : AbstractValidator<CreateAlertRuleRequest>
{
    public CreateAlertRuleRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
        RuleFor(x => x.MetricKey).NotEmpty().MaximumLength(128);
        RuleFor(x => x.WindowMinutes).InclusiveBetween(1, 1440);
        RuleFor(x => x.Threshold).Must(v => !double.IsNaN(v) && !double.IsInfinity(v));
        RuleFor(x => x.WebhookUrl).Must(u => Uri.TryCreate(u, UriKind.Absolute, out _))
            .When(x => !string.IsNullOrWhiteSpace(x.WebhookUrl))
            .WithMessage("WebhookUrl must be an absolute URL.");
    }
}
