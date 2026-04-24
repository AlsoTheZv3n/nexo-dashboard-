using Dashboard.Core.Abstractions;
using Dashboard.Core.Common;
using Dashboard.Core.Entities;

namespace Dashboard.Core.Services;

public sealed record LoginResult(User User, TokenPair Tokens);

public sealed class AuthService(
    IUserRepository users,
    IPasswordHasher hasher,
    ITokenService tokens,
    IClock clock)
{
    public async Task<Result<LoginResult>> LoginAsync(string username, string password, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return Error.Validation("Username and password are required.");

        var user = await users.GetByUsernameAsync(username, ct);
        if (user is null || !user.IsActive)
            return Error.Unauthorized("Invalid credentials.");

        if (!hasher.Verify(password, user.PasswordHash))
            return Error.Unauthorized("Invalid credentials.");

        user.RecordLogin(clock.UtcNow);
        await users.UpdateAsync(user, ct);

        var pair = tokens.CreateTokens(user);
        return new LoginResult(user, pair);
    }

    public async Task<Result<TokenPair>> RefreshAsync(string refreshToken, CancellationToken ct = default)
    {
        var userId = tokens.ValidateRefreshToken(refreshToken);
        if (userId is null)
            return Error.Unauthorized("Invalid refresh token.");

        var user = await users.GetByIdAsync(userId.Value, ct);
        if (user is null || !user.IsActive)
            return Error.Unauthorized("User is not active.");

        return tokens.CreateTokens(user);
    }
}
