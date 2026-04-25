using Dashboard.Api.Auth;
using Dashboard.Api.Contracts;
using Dashboard.Core.Abstractions;
using Dashboard.Core.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dashboard.Api.Controllers;

[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Route("api/v1/profile")]
public sealed class ProfileController(
    IUserRepository users,
    IPasswordHasher hasher,
    AuditService audit) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var id = User.RequireSubjectId();
        var user = await users.GetByIdAsync(id, ct);
        if (user is null)
            return Problem(statusCode: 404, title: "User.NotFound", detail: "Authenticated user no longer exists.");

        return Ok(new UserDto(user.Id, user.Username, user.Role.ToString(), user.IsActive, user.CreatedAt, user.LastLoginAt));
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest req, CancellationToken ct)
    {
        var id = User.RequireSubjectId();
        var user = await users.GetByIdAsync(id, ct);
        if (user is null)
            return Problem(statusCode: 404, title: "User.NotFound", detail: "Authenticated user no longer exists.");

        if (!hasher.Verify(req.CurrentPassword, user.PasswordHash))
            return Problem(statusCode: 401, title: "Unauthorized", detail: "Current password is incorrect.");

        user.ChangePassword(hasher.Hash(req.NewPassword));
        await users.UpdateAsync(user, ct);
        await audit.RecordAsync(id, "user.password_changed", "User", id.ToString(), null,
            HttpContext.Connection.RemoteIpAddress?.ToString(), ct);

        return NoContent();
    }
}
