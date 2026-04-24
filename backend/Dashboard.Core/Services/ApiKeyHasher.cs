using System.Security.Cryptography;
using System.Text;

namespace Dashboard.Core.Services;

/// <summary>
/// API keys use plain SHA-256 rather than BCrypt: we want constant-time lookup
/// (index by hash) and the key itself is already 32 random bytes — no brute-force
/// surface. Prefix = first 8 chars of the base64url-encoded random bytes, stored
/// in cleartext so the UI can help users identify their keys after creation.
/// </summary>
public static class ApiKeyHasher
{
    private const int KeyBytes = 32;

    public sealed record GeneratedKey(string PlainText, string Hash, string Prefix);

    public static GeneratedKey Generate()
    {
        var buffer = new byte[KeyBytes];
        RandomNumberGenerator.Fill(buffer);
        var plain = "nxk_" + Base64UrlEncode(buffer);
        return new GeneratedKey(plain, Hash(plain), plain[..12]);   // "nxk_" + 8 chars
    }

    public static string Hash(string plain)
    {
        var bytes = Encoding.UTF8.GetBytes(plain);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
}
