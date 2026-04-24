using System.Security.Claims;
using System.Text.Json;
using Dashboard.Api.Contracts;
using Dashboard.Core.Abstractions;
using Dashboard.Core.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dashboard.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/executions")]
public sealed class ExecutionsController(
    IExecutionRepository executions,
    IScriptRepository scripts) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = "Admin,Operator")]
    public async Task<IActionResult> Create([FromBody] CreateExecutionRequest req, CancellationToken ct)
    {
        var script = await scripts.GetByIdAsync(req.ScriptId, ct);
        if (script is null)
            return Problem(statusCode: 404, title: "Script.NotFound", detail: $"No script with id {req.ScriptId}.");

        var userId = GetUserId();
        var paramsJson = req.Parameters is null ? "{}" : JsonSerializer.Serialize(req.Parameters);
        var exec = new PsExecution(script.Id, userId, paramsJson);

        await executions.AddAsync(exec, ct);

        // Phase 3 wires real PS execution. For Phase 1 the execution stays Pending — a stub.
        return Accepted($"/api/v1/executions/{exec.Id}", ToDto(exec));
    }

    [HttpGet]
    public async Task<IActionResult> GetPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? scriptId = null,
        [FromQuery] ExecutionStatus? status = null,
        CancellationToken ct = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var (items, total) = await executions.GetPagedAsync(page, pageSize, scriptId, status, ct);
        Response.Headers["X-Total-Count"] = total.ToString();
        return Ok(new PagedResponse<ExecutionDto>(items.Select(ToDto).ToList(), page, pageSize, total));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var e = await executions.GetByIdAsync(id, ct);
        return e is null
            ? Problem(statusCode: 404, title: "Execution.NotFound", detail: $"No execution with id {id}.")
            : Ok(ToDto(e));
    }

    private Guid GetUserId()
    {
        var sub = User.FindFirstValue("sub")
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Authenticated request without subject claim.");
        return Guid.Parse(sub);
    }

    private static ExecutionDto ToDto(PsExecution e) => new(
        e.Id, e.ScriptId, e.Status.ToString(),
        e.Stdout, e.Stderr, e.ExitCode,
        e.CreatedAt, e.StartedAt, e.CompletedAt);
}
