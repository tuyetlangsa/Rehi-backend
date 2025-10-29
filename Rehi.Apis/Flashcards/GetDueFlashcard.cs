using MediatR;
using Microsoft.AspNetCore.Mvc;
using Rehi.Apis.Articles;
using Rehi.Apis.Endpoints;
using Rehi.Apis.Results;

namespace Rehi.Apis.Flashcards;

public class GetDueFlashcard : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/flashcards/review", async (ISender sender) =>
            {
                var result = await sender.Send(
                    new Application.Flashcards.GetDueFlashcard.GetDueFlashcard.Query());
                return result.MatchOk();
            })
            .WithTags("Flashcards")
            .RequireAuthorization()
            .WithName("GetDueFlashcard");
    }
}
