using MediatR;
using Microsoft.AspNetCore.Mvc;
using Rehi.Apis.Endpoints;
using Rehi.Apis.Results;

namespace Rehi.Apis.Highlights;

public class DeleteHighlight : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/highlights/{highlightId}/{updateAt}", 
                async ([FromRoute] Guid highlightId, [FromRoute] long updateAt, ISender sender) =>
                {
                    var result =
                        await sender.Send(new Application.Highlights.DeleteHighlight.DeleteHighlight.Command(highlightId, updateAt));
                    return result.MatchOk();
                })
            .WithTags("Highlights")
            .RequireAuthorization()
            .WithName("DeleteHighlight");
    }
}