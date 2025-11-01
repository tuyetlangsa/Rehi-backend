using MediatR;
using Microsoft.AspNetCore.Mvc;
using Rehi.Apis.Endpoints;
using Rehi.Apis.Results;
using Rehi.Application.Articles.CreateArticle;

namespace Rehi.Apis.Articles;

public class CreateArticleEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/articles/get-or-add", async ([FromBody] Request request, ISender sender) =>
            {

                var result = await sender.Send(new CreateArticle.Command(request.Id, request.Url,  request.RawHtml, request.Title, request.CreateAt));
                if ( result.IsSuccess)
                {
                    if (result.Value.IsSavedBefore)
                    return result.MatchOk();
                }
                return result.MatchCreated(id => $"/articles/{id}");
            })
            .WithTags("Articles")
            .RequireAuthorization()
            .WithName("CreateArticle");
    }
    internal sealed class Request
    {
        public Guid Id { get; set; }
        public string Url { get; set; }
        public string RawHtml { get; set; }
        public string Title { get; set; }
        public long CreateAt  { get; set; }
    }
}