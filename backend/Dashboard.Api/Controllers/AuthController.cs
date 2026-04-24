using Dashboard.Api.Contracts;
using Dashboard.Core.Common;
using Dashboard.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace Dashboard.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(AuthService auth, AuditService audit) : ControllerBase
{
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req, CancellationToken ct)
    {
        var result = await auth.LoginAsync(req.Username, req.Password, ct);
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

        if (result.IsFailure)
        {
            await audit.RecordAsync(null, "auth.login_failed", "User", req.Username, null, ip, ct);
            return ProblemForError(result.Error);
        }

        var (user, tokens) = result.Value!;
        await audit.RecordAsync(user.Id, "auth.login", "User", user.Id.ToString(), null, ip, ct);

        return Ok(new LoginResponse(
            tokens.AccessToken,
            tokens.RefreshToken,
            tokens.AccessExpiresAt,
            new UserInfo(user.Id, user.Username, user.Role.ToString())));
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest req, CancellationToken ct)
    {
        var result = await auth.RefreshAsync(req.RefreshToken, ct);
        if (result.IsFailure) return ProblemForError(result.Error);

        var t = result.Value!;
        return Ok(new { t.AccessToken, t.RefreshToken, t.AccessExpiresAt });
    }

    private IActionResult ProblemForError(Error error) => error.Code switch
    {
        "Unauthorized" => Problem(statusCode: StatusCodes.Status401Unauthorized, title: error.Code, detail: error.Message),
        "Validation" => ValidationProblem(error.Message),
        _ => Problem(statusCode: StatusCodes.Status400BadRequest, title: error.Code, detail: error.Message),
    };
}
