using MediatR;
using Microsoft.AspNetCore.Mvc;
using Rehi.Apis.Endpoints;
using Rehi.Apis.Results;

namespace Rehi.Apis.Articles;

public class RecoverArticle : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/articles/recover/{articleId}/{updateAt}",
                async ([FromRoute] Guid articleId, [FromRoute] long updateAt, ISender sender) =>
                {
                    var result =
                        await sender.Send(
                            new Application.Articles.RecoverArticle.RecoverArticle.Command(articleId, updateAt));
                    return result.MatchOk();
                })
            .WithTags("Articles")
            .RequireAuthorization()
            .WithName("RecoverArticle");
    }
}