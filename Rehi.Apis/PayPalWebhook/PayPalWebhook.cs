using MediatR;
using Rehi.Apis.Endpoints;
using Rehi.Apis.Results;
using Rehi.Application.Subscriptions.PayPalWebhook;

namespace Rehi.Apis.PayPalWebhook;

public class PayPalWebhook : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/paypal/webhook", async (HttpRequest request, ISender sender) =>
            {
                var result = await sender.Send(new ReceivePayPalWebhook.Command(request.Body));
                return result.MatchOk();
            })
            .WithTags("PayPal Webhook")
            .AllowAnonymous()
            .WithName("ReceivePayPalWebhook");
    }
}