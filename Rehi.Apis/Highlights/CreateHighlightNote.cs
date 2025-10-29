using MediatR;
using Microsoft.AspNetCore.Mvc;
using Rehi.Apis.Endpoints;
using Rehi.Apis.Results;

namespace Rehi.Apis.Highlights;

public class CreateHighlightNote : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/highlights/note", 
                async ([FromBody] Request request, ISender sender) =>
                {
                    var result =
                        await sender.Send(new Application.Highlights.CreateHighlightNote.CreateHighlightNote.Command(
                            request.HighlightId, 
                            request.Note, 
                            request.SavedAt));
                    return result.MatchOk();
                })
            .WithTags("Highlights")
            .RequireAuthorization()
            .WithName("SaveHighlightNote");
    }
}

public class Request
{
    public Guid HighlightId { get; set; }
    public string Note { get; set; }
    public long SavedAt { get; set; }
}