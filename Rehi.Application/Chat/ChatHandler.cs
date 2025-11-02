
using System.Net;
using Microsoft.EntityFrameworkCore;
using OpenAI.Chat;
using Rehi.Application.Abstraction.Authentication;
using Rehi.Application.Abstraction.Data;
using Rehi.Application.Abstraction.Messaging;
using Rehi.Domain.Common;
using Rehi.Domain.Users;

namespace Rehi.Application.Chat;

public abstract class ChatHandler
{    
    public record Message(string Role, string Content);

    public record Command(string Question, List<Message> History): ICommand<ChatResponse>;

    internal class Handler(IDbContext dbContext, IUserContext userContext, RagChatService ragChatService) : ICommandHandler<Command, ChatResponse>
    {
        public async Task<Result<ChatResponse>> Handle(Command command, CancellationToken cancellationToken)
        {
            var history = new List<ChatMessage>();
            
            if (command.History is not null)
            {
                foreach (var h in command.History)
                {
                    if (h.Role.Equals("user", StringComparison.OrdinalIgnoreCase))
                    {
                        history.Add(ChatMessage.CreateUserMessage(h.Content));
                    }
                    else if (h.Role.Equals("assistant", StringComparison.OrdinalIgnoreCase))
                    {
                        history.Add(ChatMessage.CreateAssistantMessage(h.Content));
                    }
                }
            }
            var chatResponse = await ragChatService.ChatAsync(command.Question, history, cancellationToken: cancellationToken);
            return chatResponse;
        }
    }
}