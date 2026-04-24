using Dashboard.Api.Auth;
using Dashboard.Api.Contracts;
using Dashboard.Core.Abstractions;
using Dashboard.Core.Entities;
using Dashboard.Core.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dashboard.Api.Controllers;

[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Route("api/v1/alerts")]
public sealed class AlertsController(
    IAlertRuleRepository rules,
    IAlertIncidentRepository incidents,
    IClock clock,
    AuditService audit) : ControllerBase
{
    [HttpGet("rules")]
    public async Task<IActionResult> GetRules(CancellationToken ct)
    {
        var all = await rules.GetAllAsync(ct);
        return Ok(all.Select(ToDto));
    }

    [HttpPost("rules")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateRule([FromBody] CreateAlertRuleRequest req, CancellationToken ct)
    {
        var rule = new AlertRule(req.Name, req.MetricKey, req.Operator, req.Threshold,
            req.WindowMinutes, req.Aggregation, req.WebhookUrl);
        await rules.AddAsync(rule, ct);
        await audit.RecordAsync(Actor(), "alert.rule_created", "AlertRule", rule.Id.ToString(), null, Ip(), ct);
        return Created($"/api/v1/alerts/rules/{rule.Id}", ToDto(rule));
    }

    [HttpDelete("rules/{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteRule(Guid id, CancellationToken ct)
    {
        var rule = await rules.GetByIdAsync(id, ct);
        if (rule is null)
            return Problem(statusCode: 404, title: "AlertRule.NotFound", detail: $"No rule with id {id}.");
        await rules.RemoveAsync(rule, ct);
        await audit.RecordAsync(Actor(), "alert.rule_deleted", "AlertRule", id.ToString(), null, Ip(), ct);
        return NoContent();
    }

    [HttpGet("incidents")]
    public async Task<IActionResult> GetIncidents([FromQuery] int limit = 50, CancellationToken ct = default)
    {
        limit = Math.Clamp(limit, 1, 500);
        var all = await incidents.GetRecentAsync(limit, ct);
        return Ok(all.Select(ToIncidentDto));
    }

    [HttpPost("incidents/{id:guid}/acknowledge")]
    [Authorize(Roles = "Admin,Operator")]
    public async Task<IActionResult> Acknowledge(Guid id, CancellationToken ct)
    {
        var incident = await incidents.GetByIdAsync(id, ct);
        if (incident is null)
            return Problem(statusCode: 404, title: "Incident.NotFound", detail: $"No incident with id {id}.");

        incident.Acknowledge(Actor(), clock.UtcNow);
        await incidents.UpdateAsync(incident, ct);
        await audit.RecordAsync(Actor(), "alert.incident_acked", "AlertIncident", id.ToString(), null, Ip(), ct);
        return Ok(ToIncidentDto(incident));
    }

    private Guid Actor() => User.RequireSubjectId();
    private string? Ip() => HttpContext.Connection.RemoteIpAddress?.ToString();

    private static AlertRuleDto ToDto(AlertRule r) => new(
        r.Id, r.Name, r.MetricKey, r.Operator.ToString(), r.Threshold, r.WindowMinutes,
        r.Aggregation.ToString(), r.WebhookUrl, r.IsActive, r.LastEvaluatedAt);

    private static AlertIncidentDto ToIncidentDto(AlertIncident i) => new(
        i.Id, i.RuleId, i.State.ToString(), i.ObservedValue, i.TriggeredAt,
        i.AcknowledgedAt, i.AcknowledgedByUserId);
}
