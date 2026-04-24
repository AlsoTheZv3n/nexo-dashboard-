using Dashboard.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Dashboard.Api.Controllers;

[ApiController]
[Route("api/v1/health")]
public sealed class HealthController(DashboardDbContext db) : ControllerBase
{
    [HttpGet("live")]
    public IActionResult Live() => Ok(new { status = "alive", at = DateTimeOffset.UtcNow });

    [HttpGet("ready")]
    public async Task<IActionResult> Ready(CancellationToken ct)
    {
        try
        {
            var canConnect = await db.Database.CanConnectAsync(ct);
            return canConnect
                ? Ok(new { status = "ready", db = "ok" })
                : StatusCode(503, new { status = "not-ready", db = "unreachable" });
        }
        catch (Exception ex)
        {
            return StatusCode(503, new { status = "not-ready", db = "error", error = ex.Message });
        }
    }
}
