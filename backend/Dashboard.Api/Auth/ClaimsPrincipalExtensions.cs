using System.Security.Claims;

namespace Dashboard.Api.Auth;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Resolves the authenticated subject id, tolerating the standard JWT "sub" claim
    /// as well as the .NET-canonical ClaimTypes.NameIdentifier form that AspNetCore's
    /// JwtBearer handler produces when the InboundClaimTypeMap is left at its defaults.
    /// Throws only if neither claim is present, which would be a genuine programming error
    /// (the surrounding Authorize attributes should have short-circuited the request).
    /// </summary>
    public static Guid RequireSubjectId(this ClaimsPrincipal user)
    {
        var sub = user.FindFirstValue("sub")
            ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (sub is null)
            throw new InvalidOperationException("Authenticated principal is missing a subject claim.");
        return Guid.Parse(sub);
    }
}
