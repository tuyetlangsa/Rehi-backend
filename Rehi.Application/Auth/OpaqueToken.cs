using System.Security.Cryptography;
using System.Text;

namespace Rehi.Application.Auth;

internal readonly record struct OpaqueToken(string Value, string Hash)
{
    public static OpaqueToken Create()
    {
        var value = Base64Url(RandomNumberGenerator.GetBytes(32));
        return new OpaqueToken(value, HashOf(value));
    }

    public static string HashOf(string value) => Base64Url(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
