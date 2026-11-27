using MediatR;
using Microsoft.AspNetCore.Mvc;
using Rehi.Apis.Endpoints;
using Rehi.Apis.Results;
using Rehi.Application.Auth;

namespace Rehi.Apis.Auth;

public class ResendVerification : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/resend-verification", async ([FromBody] Request request, ISender sender) =>
            {
                var result = await sender.Send(
                    new Application.Auth.ResendVerification.ResendVerification.Command(
                        Emails.Normalize(request.Email)));
                return result.MatchOk();
            })
            .WithTags("Auth")
            .AllowAnonymous()
            .WithName("ResendVerification");
    }

    internal sealed class Request
    {
        public string Email { get; set; } = null!;
    }
}
