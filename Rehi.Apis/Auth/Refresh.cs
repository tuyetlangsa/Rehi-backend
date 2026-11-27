using MediatR;
using Microsoft.AspNetCore.Mvc;
using Rehi.Apis.Endpoints;
using Rehi.Apis.Results;

namespace Rehi.Apis.Auth;

public class Refresh : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/refresh", async ([FromBody] Request request, ISender sender) =>
            {
                var result = await sender.Send(new Application.Auth.Refresh.Refresh.Command(request.RefreshToken));
                return result.MatchOk();
            })
            .WithTags("Auth")
            .AllowAnonymous()
            .WithName("RefreshSession");
    }

    internal sealed class Request
    {
        public string RefreshToken { get; set; } = null!;
    }
}
