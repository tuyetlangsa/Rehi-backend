using MediatR;
using Microsoft.AspNetCore.Mvc;
using Rehi.Apis.Endpoints;
using Rehi.Apis.Results;

namespace Rehi.Apis.Articles;

public class DeleteArticle : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/articles/{articleId}/{updateAt}",
                async ([FromRoute] Guid articleId, [FromRoute] long updateAt, ISender sender) =>
                {
                    var result =
                        await sender.Send(
                            new Application.Articles.DeleteArticle.DeleteArticle.Command(articleId, updateAt));
                    return result.MatchOk();
                })
            .WithTags("Articles")
            .RequireAuthorization()
            .WithName("DeleteArticle");
    }
}