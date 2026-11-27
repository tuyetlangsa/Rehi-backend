using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Rehi.Application.Abstraction.Authentication;
using Rehi.Application.Abstraction.Clock;
using Rehi.Domain.Users;

namespace Rehi.Infrastructure.Authentication;

internal sealed class TokenIssuer(IOptions<RehiJwtOptions> options, IDateTimeProvider clock) : ITokenIssuer
{
    private readonly JsonWebTokenHandler _handler = new();
    private readonly SigningCredentials _credentials = new(options.Value.SigningKey(), SecurityAlgorithms.HmacSha256);

    public AccessToken IssueAccessToken(User user)
    {
        var now = clock.UtcNow;
        var expiresAt = now.AddMinutes(options.Value.ExpirationInMinutes);

        var token = _handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = options.Value.Issuer,
            Audience = options.Value.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = expiresAt,
            Claims = new Dictionary<string, object>
            {
                ["sub"] = user.Id.ToString(),
                ["email"] = user.Email,
                ["name"] = user.FullName
            },
            SigningCredentials = _credentials
        });

        return new AccessToken(token, expiresAt);
    }
}
