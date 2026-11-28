using MediatR;
using Microsoft.AspNetCore.Mvc;
using Rehi.Apis.Endpoints;
using Rehi.Apis.Results;

namespace Rehi.Apis.Auth;

public class ResetPassword : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/reset-password", async ([FromBody] Request request, ISender sender) =>
            {
                var result = await sender.Send(
                    new Application.Auth.ResetPassword.ResetPassword.Command(request.Token, request.NewPassword));
                return result.MatchOk();
            })
            .WithTags("Auth")
            .AllowAnonymous()
            .RequireRateLimiting(AuthRateLimits.ResetPassword)
            .WithName("ResetPassword");
    }

    internal sealed class Request
    {
        public string Token { get; set; } = null!;
        public string NewPassword { get; set; } = null!;
    }
}
