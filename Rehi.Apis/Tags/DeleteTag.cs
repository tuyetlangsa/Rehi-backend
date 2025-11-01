using MediatR;
using Microsoft.AspNetCore.Mvc;
using Rehi.Apis.Endpoints;
using Rehi.Apis.Results;

namespace Rehi.Apis.Tags;

public class DeleteTag : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/tags/{name}/{updateAt}",
                async ([FromRoute] string name, [FromRoute] long updateAt, ISender sender) =>
                {
                    var result = await sender.Send(new Application.Tags.DeleteTag.Command(name, updateAt));
                    return result.MatchOk();
                })
            .WithTags("Tags")
            .RequireAuthorization()
            .WithName("DeleteTag");
    }
}