using MediatR;
using Microsoft.AspNetCore.Mvc;
using Rehi.Apis.Endpoints;
using Rehi.Application.Flashcards.FlashcardScheduler;
using Rehi.Domain.Flashcards;

namespace Rehi.Apis.Flashcards;

public class ReviewFlashcard : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/flashcards/review", async ([FromBody] Request request, ISender sender) =>
            {
                var result = await sender.Send(new FlashcardScheduler.Command(
                    request.FlashcardId, 
                    request.Feedback, 
                    request.ReviewedAt));
            })
            .WithTags("Flashcards")
            .RequireAuthorization()
            .WithName("ReviewFlashcard");
    }

    internal sealed class Request
    {
        public Guid FlashcardId { get; set; }
        public ReviewFeedback Feedback { get; set; }
        public long ReviewedAt { get; set; }
    }
}
  