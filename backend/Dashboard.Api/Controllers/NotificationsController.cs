using Dashboard.Api.Contracts;
using Dashboard.Core.Abstractions;
using Dashboard.Core.Entities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dashboard.Api.Controllers;

[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Route("api/v1/notifications")]
public sealed class NotificationsController(
    IAlertIncidentRepository incidents,
    IAlertRuleRepository rules) : ControllerBase
{
    /// <summary>
    /// Inbox view — at the moment that's just non-resolved alert incidents
    /// (Firing or Acknowledged). Future kinds (e.g. failed-execution
    /// follow-ups) plug in by appending to the items list.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] int limit = 20, CancellationToken ct = default)
    {
        limit = Math.Clamp(limit, 1, 100);

        // Pull a slightly-larger window of recent incidents and filter open ones.
        var recent = await incidents.GetRecentAsync(Math.Max(limit * 3, 50), ct);
        var open = recent.Where(i => i.State != AlertIncidentState.Resolved).Take(limit).ToList();

        var ruleIds = open.Select(i => i.RuleId).Distinct().ToList();
        var ruleById = new Dictionary<Guid, AlertRule>(ruleIds.Count);
        foreach (var id in ruleIds)
        {
            var r = await rules.GetByIdAsync(id, ct);
            if (r is not null) ruleById[id] = r;
        }

        var items = open.Select(i =>
        {
            ruleById.TryGetValue(i.RuleId, out var rule);
            var ruleName = rule?.Name ?? "(unknown rule)";
            var metricKey = rule?.MetricKey ?? "?";
            var op = rule?.Operator.ToString() ?? "?";
            var threshold = rule?.Threshold ?? 0;
            return new NotificationDto(
                id: i.Id,
                kind: "alert",
                title: $"Alert firing: {ruleName}",
                body: $"{metricKey} {Symbol(op)} {threshold} (observed {i.ObservedValue:0.##})",
                severity: i.State == AlertIncidentState.Firing ? "critical" : "info",
                triggeredAt: i.TriggeredAt,
                linkPath: "/alerts");
        }).ToList();

        var unreadCount = items.Count(n => n.severity == "critical");
        return Ok(new NotificationsResponse(items, unreadCount));
    }

    private static string Symbol(string op) => op switch
    {
        "GreaterThan" => ">",
        "LessThan" => "<",
        "Equals" => "=",
        _ => op,
    };
}
