using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Rehi.Application.Abstraction.Paypal;
using Rehi.Domain.Subscription;
using Rehi.Domain.Webhook;

namespace Rehi.Infrastructure.Payment.Paypal;

public class PayPalWebhookService(
    ILogger<PayPalWebhookService> logger,
    IConfiguration configuration) : IPayPalWebHookService
{
    private readonly PayPalSettings _payPalSettings =
        configuration.GetSection("PayPalSettings").Get<PayPalSettings>()
        ?? throw new ArgumentNullException(nameof(PayPalSettings));

    public async Task<PayPalWebhookResponse> ReceivePayPalWebhook(
        string subscriptionId,
        string eventType
    )
    {
        try
        {
            if (string.IsNullOrEmpty(eventType) || string.IsNullOrEmpty(subscriptionId))
            {
                logger.LogWarning("Missing event_type or subscription id in webhook payload");
                return new PayPalWebhookResponse("Invalid");
            }

            logger.LogInformation("Received PayPal event {EventType} for subscription {SubscriptionId}", eventType,
                subscriptionId);

            return new PayPalWebhookResponse("Success", eventType, subscriptionId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing PayPal webhook");
            return new PayPalWebhookResponse("Error");
        }
    }
}