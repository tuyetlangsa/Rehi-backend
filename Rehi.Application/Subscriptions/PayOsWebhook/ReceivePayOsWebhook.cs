using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Rehi.Application.Abstraction.Data;
using Rehi.Application.Abstraction.Messaging;
using Rehi.Application.Abstraction.PayOs;
using Rehi.Domain.Common;
using Rehi.Domain.Subscription;
using Rehi.Domain.Users;

namespace Rehi.Application.Subscriptions.PayOsWebhook;

public class ReceivePayOsWebhook
{
    public record Command(Stream RawBody) : ICommand<Response>;

    public record Response(string Status);

    internal class Handler(
        IDbContext dbContext,
        ILogger<ReceivePayOsWebhook> logger,
        IPayOsWebhookService payOsWebhookService
    ) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var result = await payOsWebhookService.ReceivePayOsWebhook(request.RawBody);
            if (result.Success)
            {
                return new Response("success");
            }
            else
            {
                return new Response("failed");
            }
        }
    }
}
