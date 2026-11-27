using System.Security.Claims;
using MediatR;
using Rehi.Apis.Endpoints;
using Rehi.Apis.Results;
using Rehi.Application.Auth.ExchangeAuth0;
using Rehi.Infrastructure.Authentication;

namespace Rehi.Apis.Auth;

public class Auth0Exchange : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/auth0-exchange", async (ClaimsPrincipal user, ISender sender) =>
            {
                var result = await sender.Send(new ExchangeAuth0.Command(
                    user.FindFirstValue(ClaimTypes.Email),
                    user.FindFirstValue("name"),
                    EmailVerified(user)));
                return result.MatchOk();
            })
            .WithTags("Auth")
            .RequireAuthorization(AuthPolicies.Auth0Token)
            .WithName("ExchangeAuth0");
    }

    private static bool? EmailVerified(ClaimsPrincipal user) =>
        user.FindFirstValue("email_verified") is { } value ? bool.TryParse(value, out var verified) && verified : null;
}
