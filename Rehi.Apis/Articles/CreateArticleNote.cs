using MediatR;
using Microsoft.AspNetCore.Mvc;
using Rehi.Apis.Endpoints;
using Rehi.Apis.Results;

namespace Rehi.Apis.Articles;

public class CreateArticleNote : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/articles/note",
                async ([FromBody] Request request, ISender sender) =>
                {
                    var result =
                        await sender.Send(new Application.Articles.CreateArticleNote.CreateArticleNote.Command(
                            request.ArticleId,
                            request.Note,
                            request.SavedAt));
                    return result.MatchOk();
                })
            .WithTags("Articles")
            .RequireAuthorization()
            .WithName("SaveArticleNote");
    }
}

public class Request
{
    public Guid ArticleId { get; set; }
    public string Note { get; set; }
    public long SavedAt { get; set; }
}