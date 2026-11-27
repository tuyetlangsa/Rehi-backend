using MediatR;
using Microsoft.AspNetCore.Mvc;
using Rehi.Apis.Endpoints;
using Rehi.Apis.Results;

namespace Rehi.Apis.Auth;

public class VerifyEmail : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/verify-email", async ([FromBody] Request request, ISender sender) =>
            {
                var result = await sender.Send(new Application.Auth.VerifyEmail.VerifyEmail.Command(request.Token));
                return result.MatchOk();
            })
            .WithTags("Auth")
            .AllowAnonymous()
            .WithName("VerifyEmail");
    }

    internal sealed class Request
    {
        public string Token { get; set; } = null!;
    }
}
