using MediatR;
using Microsoft.AspNetCore.Mvc;
using Rehi.Apis.Endpoints;
using Rehi.Apis.Results;
using Rehi.Application.Auth;
using Rehi.Domain.Users;

namespace Rehi.Apis.Auth;

public class Login : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/login", async ([FromBody] Request request, ISender sender) =>
            {
                var result = await sender.Send(new Application.Auth.Login.Login.Command(
                    Emails.Normalize(request.Email),
                    request.Password,
                    request.Client switch
                    {
                        "web" => RefreshTokenClient.Web,
                        "extension" => RefreshTokenClient.Extension,
                        _ => null
                    }));
                return result.MatchOk();
            })
            .WithTags("Auth")
            .AllowAnonymous()
            .WithName("Login");
    }

    internal sealed class Request
    {
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string Client { get; set; } = null!;
    }
}
