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
[Route("api/v1/users")]
public sealed class UsersController(
    IUserRepository users,
    IPasswordHasher hasher,
    AuditService audit) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var all = await users.GetAllAsync(ct);
        return Ok(all.Select(ToDto));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest req, CancellationToken ct)
    {
        var existing = await users.GetByUsernameAsync(req.Username, ct);
        if (existing is not null)
            return Problem(statusCode: 409, title: "User.Exists", detail: $"Username '{req.Username}' is taken.");

        var user = new User(req.Username, hasher.Hash(req.Password), req.Role);
        await users.AddAsync(user, ct);
        await audit.RecordAsync(Actor(), "user.created", "User", user.Id.ToString(), null, Ip(), ct);
        return Created($"/api/v1/users/{user.Id}", ToDto(user));
    }

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserRequest req, CancellationToken ct)
    {
        var user = await users.GetByIdAsync(id, ct);
        if (user is null)
            return Problem(statusCode: 404, title: "User.NotFound", detail: $"No user with id {id}.");

        if (req.IsActive is false && id == Actor())
            return Problem(statusCode: 400, title: "User.SelfLock",
                detail: "Admins cannot deactivate their own account.");

        if (req.Role.HasValue) user.ChangeRole(req.Role.Value);
        if (req.IsActive.HasValue)
        {
            if (req.IsActive.Value) user.Activate();
            else user.Deactivate();
        }
        await users.UpdateAsync(user, ct);
        await audit.RecordAsync(Actor(), "user.updated", "User", user.Id.ToString(),
            System.Text.Json.JsonSerializer.Serialize(req), Ip(), ct);
        return Ok(ToDto(user));
    }

    [HttpPost("{id:guid}/reset-password")]
    public async Task<IActionResult> ResetPassword(Guid id, [FromBody] ResetPasswordRequest req, CancellationToken ct)
    {
        var user = await users.GetByIdAsync(id, ct);
        if (user is null)
            return Problem(statusCode: 404, title: "User.NotFound", detail: $"No user with id {id}.");

        user.ChangePassword(hasher.Hash(req.NewPassword));
        await users.UpdateAsync(user, ct);
        await audit.RecordAsync(Actor(), "user.password_reset", "User", user.Id.ToString(), null, Ip(), ct);
        return NoContent();
    }

    private Guid Actor() => User.RequireSubjectId();
    private string? Ip() => HttpContext.Connection.RemoteIpAddress?.ToString();

    private static UserDto ToDto(User u) =>
        new(u.Id, u.Username, u.Role.ToString(), u.IsActive, u.CreatedAt, u.LastLoginAt);
}
