using MediatR;
using Microsoft.AspNetCore.Mvc;
using Rehi.Apis.Endpoints;
using Rehi.Apis.Results;
using Rehi.Application.Highlights.CreateHighlight;

namespace Rehi.Apis.Highlights;

public class CreateHighlightEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/highlights", async ([FromBody] Request request, ISender sender) =>
            {
                var result = await sender.Send(new CreateHighlight.Command(
                    request.Id,
                    request.Location,
                    request.Html,
                    request.Markdown,
                    request.PlainText,
                    request.ArticleId,
                    request.CreateAt,
                    request.Color,
                    request.CreateBy
                ));

                return result.MatchCreated(id => $"/highlights/{id}");
            })
            .WithTags("Highlights")
            .RequireAuthorization()
            .WithName("CreateHighlight");
    }

    internal sealed class Request
    {
        public Guid Id { get; set; }
        public string Location { get; set; }
        public string Html { get; set; }
        public string Markdown { get; set; }
        public string PlainText { get; set; }
        public Guid ArticleId { get; set; }
        public string Color { get; set; }
        public long CreateAt { get; set; }
        public string CreateBy { get; set; }
    }
}