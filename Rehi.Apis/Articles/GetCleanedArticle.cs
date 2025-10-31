using MediatR;
using Microsoft.AspNetCore.Mvc;
using Rehi.Apis.Endpoints;
using Rehi.Apis.Results;

namespace Rehi.Apis.Articles;

public class GetCleanedArticle : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/articles/{articleId}/document", async ([FromRoute] Guid articleId, ISender sender) =>
            {
                var result =
                    await sender.Send(new Application.Articles.GetCleanedArticle.GetCleanedArticle.Query(articleId));
                return result.MatchOk();
            })
            .WithTags("Articles")
            .RequireAuthorization()
            .WithName("GetCleanedArticle");
    }
}