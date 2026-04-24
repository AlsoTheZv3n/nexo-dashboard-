using Dashboard.Api.Contracts;
using Dashboard.Core.Abstractions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dashboard.Api.Controllers;

[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
[Route("api/v1/audit")]
public sealed class AuditController(IAuditLogRepository log) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] Guid? userId = null,
        [FromQuery] string? action = null,
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        CancellationToken ct = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 500);
        var (items, total) = await log.GetPagedAsync(page, pageSize, userId, action, from, to, ct);
        Response.Headers["X-Total-Count"] = total.ToString();
        return Ok(new PagedResponse<AuditLogEntryDto>(
            items.Select(e => new AuditLogEntryDto(
                e.Id, e.UserId, e.Action, e.TargetType, e.TargetId, e.DetailsJson, e.IpAddress, e.Timestamp))
                .ToList(),
            page, pageSize, total));
    }
}
