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
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
[Route("api/v1/api-keys")]
public sealed class ApiKeysController(
    IApiKeyRepository apiKeys,
    IClock clock,
    AuditService audit) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var all = await apiKeys.GetAllAsync(ct);
        return Ok(all.Select(ToDto));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateApiKeyRequest req, CancellationToken ct)
    {
        var userId = User.RequireSubjectId();
        var expires = req.ExpiresInDays.HasValue ? clock.UtcNow.AddDays(req.ExpiresInDays.Value) : (DateTimeOffset?)null;
        var generated = ApiKeyHasher.Generate();
        var key = new ApiKey(req.Name, generated.Hash, generated.Prefix, userId, req.Role, expires);
        await apiKeys.AddAsync(key, ct);

        await audit.RecordAsync(userId, "apikey.created", "ApiKey", key.Id.ToString(), null,
            HttpContext.Connection.RemoteIpAddress?.ToString(), ct);

        return Created($"/api/v1/api-keys/{key.Id}",
            new ApiKeyCreatedDto(ToDto(key), generated.PlainText));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Revoke(Guid id, CancellationToken ct)
    {
        var key = await apiKeys.GetByIdAsync(id, ct);
        if (key is null)
            return Problem(statusCode: 404, title: "ApiKey.NotFound", detail: $"No API key with id {id}.");

        key.Revoke();
        await apiKeys.UpdateAsync(key, ct);

        var actor = User.RequireSubjectId();

        await audit.RecordAsync(actor, "apikey.revoked", "ApiKey", key.Id.ToString(), null,
            HttpContext.Connection.RemoteIpAddress?.ToString(), ct);

        return NoContent();
    }

    private static ApiKeyDto ToDto(ApiKey k) => new(
        k.Id, k.Name, k.Prefix, k.Role.ToString(), k.IsActive,
        k.CreatedAt, k.ExpiresAt, k.LastUsedAt);
}
