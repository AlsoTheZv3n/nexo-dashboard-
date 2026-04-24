using Dashboard.Api.Contracts;
using Dashboard.Core.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dashboard.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/scripts")]
public sealed class ScriptsController(IScriptRepository scripts) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var all = await scripts.GetAllAsync(ct);
        return Ok(all.Select(s => new ScriptDto(s.Id, s.Name, s.Description, s.FilePath, s.MetaJson, s.UpdatedAt)));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var s = await scripts.GetByIdAsync(id, ct);
        return s is null
            ? Problem(statusCode: 404, title: "Script.NotFound", detail: $"No script with id {id}.")
            : Ok(new ScriptDto(s.Id, s.Name, s.Description, s.FilePath, s.MetaJson, s.UpdatedAt));
    }
}
