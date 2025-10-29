using Rehi.Domain.Webhook;

namespace Rehi.Application.Abstraction.Paypal;

public interface IPayPalWebHookService
{
    Task<PayPalWebhookResponse> ReceivePayPalWebhook(
        string payload,
        string transmissionId,
        string transmissionTime,
        string transmissionSig,
        string certUrl);
}