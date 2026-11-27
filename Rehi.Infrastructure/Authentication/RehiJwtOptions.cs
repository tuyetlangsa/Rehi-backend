using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Rehi.Infrastructure.Authentication;

public sealed class RehiJwtOptions
{
    public const string Section = "Jwt";
    public const int MinimumSecretBytes = 32;

    public string Secret { get; init; } = "";
    public string Issuer { get; init; } = "";
    public string Audience { get; init; } = "";
    public int ExpirationInMinutes { get; init; }

    public SymmetricSecurityKey SigningKey() => new(Encoding.UTF8.GetBytes(Secret));
}
