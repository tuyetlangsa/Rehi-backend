using MediatR;
using Microsoft.AspNetCore.Mvc;
using Rehi.Apis.Endpoints;
using Rehi.Apis.Results;
using Rehi.Application.Articles.CreateArticle;
using Rehi.Domain.Common;

namespace Rehi.Apis.Articles;

public class CreateArticleEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/articles", async ([FromBody] Request request, ISender sender) =>
            {
                Result<Guid> result = await sender.Send(new CreateArticle.Command(request.Title, request.RawHtml));
                return result.MatchCreated(id => $"/articles/{id}");
            })
            .WithTags("Articles")
            .WithName("CreateArticle");
        
        app.MapGet("/article" , async () => "Hello");
    }
    internal sealed class Request
    {
        public string Title { get; set; }
        public string RawHtml { get; set; }
    }
    
}