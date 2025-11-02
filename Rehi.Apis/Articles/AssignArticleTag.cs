using MediatR;
using Microsoft.AspNetCore.Mvc;
using Rehi.Apis.Endpoints;
using Rehi.Apis.Results;

namespace Rehi.Apis.Articles;

public class AssignArticleTag : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/articles/assign-tag", async ([FromBody] Request request,  ISender sender) =>
            {
                var result = await sender.Send(
                    new Application.Articles.AssignArticleTag.AssignArticleTag.Command(
                        request.ArticleId, 
                        request.TagName, 
                        request.CreateAt));
                return result.MatchOk();
            })
            .WithTags("Articles")
            .RequireAuthorization()
            .WithName("AssignArticleTag");
    }
    
    
    public class Request
    {
        public Guid ArticleId { get; set; }
        public string TagName { get; set; }
        public long CreateAt { get; set; }
    }
}