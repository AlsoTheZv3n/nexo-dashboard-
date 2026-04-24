using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Dashboard.Core.Abstractions;
using Dashboard.Core.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Dashboard.Infrastructure.Auth;

public sealed class JwtTokenService : ITokenService
{
    private const string RefreshPurpose = "refresh";
    private readonly JwtOptions _opts;
    private readonly IClock _clock;

    static JwtTokenService()
    {
        // Keep the original JWT claim names ("sub", "iss", "aud", …) on the
        // validated principal instead of silently remapping to ClaimTypes.* URIs.
        JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
    }

    public JwtTokenService(IOptions<JwtOptions> options, IClock clock)
    {
        _opts = options.Value;
        _clock = clock;
    }

    public TokenPair CreateTokens(User user)
    {
        var now = _clock.UtcNow;
        var accessExpires = now.AddMinutes(_opts.AccessTokenMinutes);

        var access = WriteJwt(
            claims: [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim("purpose", "access")
            ],
            expires: accessExpires);

        var refresh = WriteJwt(
            claims: [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim("purpose", RefreshPurpose)
            ],
            expires: now.AddDays(_opts.RefreshTokenDays));

        return new TokenPair(access, refresh, accessExpires);
    }

    public Guid? ValidateRefreshToken(string refreshToken)
    {
        var handler = new JwtSecurityTokenHandler();
        try
        {
            var principal = handler.ValidateToken(refreshToken, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _opts.Issuer,
                ValidateAudience = true,
                ValidAudience = _opts.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = SigningKey(),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(1)
            }, out _);

            if (principal.FindFirst("purpose")?.Value != RefreshPurpose) return null;
            var sub = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            return Guid.TryParse(sub, out var id) ? id : null;
        }
        catch (SecurityTokenException)
        {
            return null;
        }
    }

    private string WriteJwt(IEnumerable<Claim> claims, DateTimeOffset expires)
    {
        var creds = new SigningCredentials(SigningKey(), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _opts.Issuer,
            audience: _opts.Audience,
            claims: claims,
            expires: expires.UtcDateTime,
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private SymmetricSecurityKey SigningKey() => new(Encoding.UTF8.GetBytes(_opts.SigningKey));
}
