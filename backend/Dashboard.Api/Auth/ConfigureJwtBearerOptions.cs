using System.Text;
using Dashboard.Infrastructure.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Dashboard.Api.Auth;

/// <summary>
/// Binds JwtBearer validation to <see cref="JwtOptions"/> at DI-resolution time.
/// Important because WebApplicationFactory may replace configuration AFTER Program.cs
/// would otherwise capture snapshot values at startup.
/// </summary>
public sealed class ConfigureJwtBearerOptions(IOptions<JwtOptions> jwt)
    : IPostConfigureOptions<JwtBearerOptions>
{
    public void PostConfigure(string? name, JwtBearerOptions options)
    {
        if (name != JwtBearerDefaults.AuthenticationScheme) return;

        var j = jwt.Value;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = j.Issuer,
            ValidateAudience = true,
            ValidAudience = j.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(j.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    }
}
