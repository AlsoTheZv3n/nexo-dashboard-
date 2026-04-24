using System.Security.Claims;
using System.Text.Encodings.Web;
using Dashboard.Core.Abstractions;
using Dashboard.Core.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dashboard.Api.Auth;

public static class ApiKeyAuthentication
{
    public const string Scheme = "ApiKey";
    public const string HeaderName = "Authorization";
    public const string HeaderPrefix = "ApiKey ";
}

public sealed class ApiKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IApiKeyRepository apiKeys,
    IClock clock)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(ApiKeyAuthentication.HeaderName, out var header))
        {
            return AuthenticateResult.NoResult();
        }
        var raw = header.ToString();
        if (!raw.StartsWith(ApiKeyAuthentication.HeaderPrefix, StringComparison.Ordinal))
        {
            return AuthenticateResult.NoResult();
        }

        var plain = raw[ApiKeyAuthentication.HeaderPrefix.Length..].Trim();
        if (string.IsNullOrEmpty(plain))
        {
            return AuthenticateResult.Fail("Empty API key.");
        }

        var hash = ApiKeyHasher.Hash(plain);
        var key = await apiKeys.FindByHashAsync(hash, Context.RequestAborted);
        if (key is null || !key.IsUsable(clock.UtcNow))
        {
            return AuthenticateResult.Fail("Invalid or expired API key.");
        }

        key.RecordUse(clock.UtcNow);
        await apiKeys.UpdateAsync(key, CancellationToken.None);

        var identity = new ClaimsIdentity(ApiKeyAuthentication.Scheme);
        identity.AddClaim(new Claim("sub", key.CreatedByUserId.ToString()));
        identity.AddClaim(new Claim(ClaimTypes.Name, $"apikey:{key.Prefix}"));
        identity.AddClaim(new Claim(ClaimTypes.Role, key.Role.ToString()));
        identity.AddClaim(new Claim("auth_method", "apikey"));
        identity.AddClaim(new Claim("api_key_id", key.Id.ToString()));

        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, ApiKeyAuthentication.Scheme);
        return AuthenticateResult.Success(ticket);
    }
}
