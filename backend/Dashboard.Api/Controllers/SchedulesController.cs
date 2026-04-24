using System.Text.Json;
using Dashboard.Api.Auth;
using Dashboard.Api.Contracts;
using Dashboard.Core.Abstractions;
using Dashboard.Core.Entities;
using Dashboard.Core.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NCrontab;

namespace Dashboard.Api.Controllers;

[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Route("api/v1/schedules")]
public sealed class SchedulesController(
    IScheduleRepository schedules,
    IScriptRepository scripts,
    IClock clock,
    AuditService audit) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var all = await schedules.GetAllAsync(ct);
        return Ok(all.Select(ToDto));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Operator")]
    public async Task<IActionResult> Create([FromBody] CreateScheduleRequest req, CancellationToken ct)
    {
        var script = await scripts.GetByIdAsync(req.ScriptId, ct);
        if (script is null)
            return Problem(statusCode: 404, title: "Script.NotFound", detail: $"No script with id {req.ScriptId}.");

        var cron = CrontabSchedule.TryParse(req.CronExpression);
        if (cron is null)
            return Problem(statusCode: 400, title: "Validation", detail: "Invalid cron expression.");

        var next = cron.GetNextOccurrence(DateTime.UtcNow);
        var schedule = new ScheduledExecution(
            req.ScriptId,
            req.Name,
            req.CronExpression,
            req.Parameters is null ? "{}" : JsonSerializer.Serialize(req.Parameters),
            Actor(),
            new DateTimeOffset(next, TimeSpan.Zero));
        await schedules.AddAsync(schedule, ct);
        await audit.RecordAsync(Actor(), "schedule.created", "ScheduledExecution", schedule.Id.ToString(), null, Ip(), ct);
        return Created($"/api/v1/schedules/{schedule.Id}", ToDto(schedule));
    }

    [HttpPatch("{id:guid}/toggle")]
    [Authorize(Roles = "Admin,Operator")]
    public async Task<IActionResult> Toggle(Guid id, [FromBody] ToggleScheduleRequest req, CancellationToken ct)
    {
        var schedule = await schedules.GetByIdAsync(id, ct);
        if (schedule is null)
            return Problem(statusCode: 404, title: "Schedule.NotFound", detail: $"No schedule with id {id}.");

        schedule.SetActive(req.IsActive);
        if (req.IsActive && schedule.NextRunAt is null)
        {
            var cron = CrontabSchedule.Parse(schedule.CronExpression);
            schedule.RecordRun(clock.UtcNow, new DateTimeOffset(cron.GetNextOccurrence(DateTime.UtcNow), TimeSpan.Zero));
        }
        await schedules.UpdateAsync(schedule, ct);
        await audit.RecordAsync(Actor(), "schedule.toggled", "ScheduledExecution", id.ToString(),
            req.IsActive ? "active" : "paused", Ip(), ct);
        return Ok(ToDto(schedule));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,Operator")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var schedule = await schedules.GetByIdAsync(id, ct);
        if (schedule is null)
            return Problem(statusCode: 404, title: "Schedule.NotFound", detail: $"No schedule with id {id}.");
        await schedules.RemoveAsync(schedule, ct);
        await audit.RecordAsync(Actor(), "schedule.deleted", "ScheduledExecution", id.ToString(), null, Ip(), ct);
        return NoContent();
    }

    private Guid Actor() => User.RequireSubjectId();
    private string? Ip() => HttpContext.Connection.RemoteIpAddress?.ToString();

    private static ScheduleDto ToDto(ScheduledExecution s) => new(
        s.Id, s.ScriptId, s.Name, s.CronExpression, s.ParametersJson, s.IsActive,
        s.LastRunAt, s.NextRunAt);
}
