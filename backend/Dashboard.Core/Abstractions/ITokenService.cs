using Dashboard.Core.Entities;

namespace Dashboard.Core.Abstractions;

public sealed record TokenPair(string AccessToken, string RefreshToken, DateTimeOffset AccessExpiresAt);

public interface ITokenService
{
    TokenPair CreateTokens(User user);
    Guid? ValidateRefreshToken(string refreshToken);
}
