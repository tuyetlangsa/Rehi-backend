using MediatR;
using Microsoft.AspNetCore.Mvc;
using Rehi.Apis.Endpoints;
using Rehi.Apis.Results;
using Rehi.Application.Chat;

namespace Rehi.Apis.Chat;


public class ChatWithArticle : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("chat-with-article/{question}", async ([FromBody] ChatWithHistoryRequest request,  ISender sender) =>
            {
                var history = request.History!.Select(h => new ChatWithArticleHandler.Message(h.Role, h.Content)).ToList();
                var result = await sender.Send(new ChatWithArticleHandler.Command(request.ArticleId, request.Question, history ));
                return result.MatchOk();
            })
            .WithTags("Chat")
            .WithName("Chat-with-article");
    }
    
    public record ChatWithHistoryRequest(
        Guid ArticleId,
        string Question,
        List<Message>? History = null
    );

    public record Message(string Role, string Content);
    
}