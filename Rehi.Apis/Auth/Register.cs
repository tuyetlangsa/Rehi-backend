using MediatR;
using Microsoft.AspNetCore.Mvc;
using Rehi.Apis.Endpoints;
using Rehi.Apis.Results;
using Rehi.Application.Auth;

namespace Rehi.Apis.Auth;

public class Register : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/register", async ([FromBody] Request request, ISender sender) =>
            {
                var result = await sender.Send(new Application.Auth.Register.Register.Command(
                    Emails.Normalize(request.Email),
                    request.Password,
                    request.FullName));
                return result.MatchOk();
            })
            .WithTags("Auth")
            .AllowAnonymous()
            .WithName("Register");
    }

    internal sealed class Request
    {
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string FullName { get; set; } = null!;
    }
}
