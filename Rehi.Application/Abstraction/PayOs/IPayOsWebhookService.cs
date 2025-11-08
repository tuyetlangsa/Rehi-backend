using Rehi.Domain.Webhook;

namespace Rehi.Application.Abstraction.PayOs;

public interface IPayOsWebhookService
{
        Task<PayOsWebhookResponse> ReceivePayOsWebhook(string subscriptionId, string eventType);
    
}