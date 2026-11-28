using MediatR;
using Microsoft.AspNetCore.Mvc;
using Rehi.Apis.Endpoints;
using Rehi.Apis.Results;
using Rehi.Application.Auth;

namespace Rehi.Apis.Auth;

public class ForgotPassword : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/forgot-password", async ([FromBody] Request request, ISender sender) =>
            {
                var result = await sender.Send(
                    new Application.Auth.ForgotPassword.ForgotPassword.Command(Emails.Normalize(request.Email)));
                return result.MatchOk();
            })
            .WithTags("Auth")
            .AllowAnonymous()
            .RequireRateLimiting(AuthRateLimits.ForgotPassword)
            .WithName("ForgotPassword");
    }

    internal sealed class Request
    {
        public string Email { get; set; } = null!;
    }
}
