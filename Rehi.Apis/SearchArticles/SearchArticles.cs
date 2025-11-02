using MediatR;
using Microsoft.AspNetCore.Mvc;
using Rehi.Apis.Endpoints;
using Rehi.Apis.Results;
using Rehi.Application.FulltextSearch;

namespace Rehi.Apis.SearchArticles;

public class SearchArticles : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/search", async ([FromQuery] string searchText, ISender sender) =>
            {
                var result = await sender.Send(new SearchArticle.Query(searchText));
                return result.MatchOk();
            })
            .WithTags("Articles")
            .RequireAuthorization()
            .WithName("SearchArticles");
    }
}