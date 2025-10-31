using Rehi.Domain.Webhook;

namespace Rehi.Application.Abstraction.Paypal;

public interface IPayPalWebHookService
{
    Task<PayPalWebhookResponse> ReceivePayPalWebhook(string subscriptionId, string eventType);
}