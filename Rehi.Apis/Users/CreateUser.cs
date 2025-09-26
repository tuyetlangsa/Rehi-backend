using MediatR;
using Microsoft.AspNetCore.Mvc;
using Rehi.Apis.Endpoints;
using Rehi.Apis.Results;

namespace Rehi.Apis.Users;

public class CreateUser : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/users", async ([FromBody] Request request, ISender sender) =>
            {
                var result = await sender.Send(new Application.Users.CreateUser.Command(request.Email, request.FullName));
                return result.MatchOk();
            })
            .WithTags("Users")
            .RequireAuthorization()
            .WithName("CreateUser");
    }
    internal sealed class Request
    {
        public string Email { get; set; }
        public string FullName { get; set; }
    }
}