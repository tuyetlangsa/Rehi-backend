using MediatR;
using Microsoft.AspNetCore.Mvc;
using Rehi.Apis.Endpoints;
using Rehi.Apis.Results;

namespace Rehi.Apis.Tags;

public class CreateTag : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/tags", async ([FromBody] Request request, ISender sender) =>
            {
                var result = await sender.Send(new Application.Tags.CreateTag.Command(request.Name, request.CreateAt));
                return result.MatchCreated(id => $"/tags/{id}");
            })
            .WithTags("Tags")
            .RequireAuthorization()
            .WithName("CreateTag");
    }

    internal sealed class Request
    {
        public string Name { get; set; }
        public long CreateAt { get; set; }
    }
}