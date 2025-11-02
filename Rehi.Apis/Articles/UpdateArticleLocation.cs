using MediatR;
using Microsoft.AspNetCore.Mvc;
using Rehi.Apis.Endpoints;
using Rehi.Apis.Results;

namespace Rehi.Apis.Articles;

public class UpdateArticleLocation : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/articles/{articleId}/{location}/{updateAt}",
                async ([FromRoute] Guid articleId, [FromRoute] string location, [FromRoute] long updateAt,
                    ISender sender) =>
                {
                    var result =
                        await sender.Send(
                            new Application.Articles.UpdateArticleLocation.UpdateArticleLocation.Command(articleId,
                                location, updateAt));
                    return result.MatchOk();
                })
            .WithTags("Articles")
            .RequireAuthorization()
            .WithName("UpdateArticleLocation");
    }
}