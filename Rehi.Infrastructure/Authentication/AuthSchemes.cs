using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Rehi.Infrastructure.Authentication;

public static class AuthSchemes
{
    public const string Default = JwtBearerDefaults.AuthenticationScheme;
    public const string Rehi = "Rehi";
    public const string Auth0 = "Auth0";

    private static readonly JsonWebTokenHandler TokenReader = new();

    internal static string SelectByIssuer(HttpContext context)
    {
        var header = context.Request.Headers.Authorization.ToString();
        if (!header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) return Auth0;

        var token = header["Bearer ".Length..].Trim();
        string issuer;
        try
        {
            if (!TokenReader.CanReadToken(token)) return Auth0;
            issuer = TokenReader.ReadJsonWebToken(token).Issuer;
        }
        catch (Exception)
        {
            return Auth0;
        }

        var rehiIssuer = context.RequestServices.GetRequiredService<IOptions<RehiJwtOptions>>().Value.Issuer;
        return issuer == rehiIssuer ? Rehi : Auth0;
    }
}
