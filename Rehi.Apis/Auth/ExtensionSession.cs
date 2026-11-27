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
        app.MapPost("/auth/extension-session", async (ISender sender) =>
                (await sender.Send(new CreateExtensionSession.Command())).MatchOk())
            .WithTags("Auth")
            .RequireAuthorization(AuthPolicies.RehiToken)
            .WithName("CreateExtensionSession");
    }
}
