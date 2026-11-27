using MediatR;
using Microsoft.AspNetCore.Mvc;
using Rehi.Apis.Endpoints;
using Rehi.Apis.Results;

namespace Rehi.Apis.Auth;

public class Logout : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/logout", async ([FromBody] Request request, ISender sender) =>
            {
                var result = await sender.Send(new Application.Auth.Logout.Logout.Command(request.RefreshToken));
                return result.MatchOk();
            })
            .WithTags("Auth")
            .AllowAnonymous()
            .WithName("Logout");
    }

    internal sealed class Request
    {
        public string RefreshToken { get; set; } = null!;
    }
}
