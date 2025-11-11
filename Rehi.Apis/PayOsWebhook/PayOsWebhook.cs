using MediatR;
using Rehi.Apis.Endpoints;
using Rehi.Apis.Results;
using Rehi.Application.Subscriptions.PayOsWebhook;
using Rehi.Application.Subscriptions.PayPalWebhook;

namespace Rehi.Apis.PayOsWebhook;

public class PayOsWebhook : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/payos/webhook", async (HttpRequest request,ISender sender) =>
            {
                var result = await sender.Send(new ReceivePayOsWebhook.Command(request.Body));
                return result.MatchOk();
            })
            .WithTags("PayOs Webhook")
            .AllowAnonymous()
            .WithName("ReceivePayOsWebhook");
    }
}