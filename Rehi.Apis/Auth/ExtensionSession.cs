using System.Security.Claims;
using MediatR;
using Rehi.Apis.Endpoints;
using Rehi.Apis.Results;
using Rehi.Application.Auth.CreateExtensionSession;
using Rehi.Infrastructure.Authentication;

namespace Rehi.Apis.Auth;

public class ExtensionSession : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/extension-session", async (ClaimsPrincipal user, ISender sender) =>
                (await sender.Send(new CreateExtensionSession.Command(SessionId(user)))).MatchOk())
            .WithTags("Auth")
            .RequireAuthorization(AuthPolicies.RehiToken)
            .WithName("CreateExtensionSession");
    }

    // JwtBearer maps the token's "sid" claim to ClaimTypes.Sid.
    private static Guid? SessionId(ClaimsPrincipal user) =>
        Guid.TryParse(user.FindFirstValue(ClaimTypes.Sid), out var sessionId) ? sessionId : null;
}
