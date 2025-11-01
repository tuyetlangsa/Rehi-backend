using MediatR;
using Microsoft.AspNetCore.Mvc;
using Rehi.Apis.Endpoints;
using Rehi.Apis.Results;
using Rehi.Application.Chat;

namespace Rehi.Apis.Chat;

public class Chat : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("chat/{question}", async ([FromBody] ChatWithHistoryRequest request,  ISender sender) =>
            {
                var history = request.History!.Select(h => new ChatHandler.Message(h.Role, h.Content)).ToList();
                var result = await sender.Send(new ChatHandler.Command(request.Question, history ));
                return result.MatchOk();
            })
            .WithTags("Chat")
            .WithName("Chat-with-history");
    }
    
    public record ChatWithHistoryRequest(
        string Question,
        List<Message>? History = null
    );

    public record Message(string Role, string Content);
    
}