using Rehi.Domain.Common;

namespace Rehi.Domain.Webhook;

public class PayPalWebhookErrors
{
    public static Error NotFound => new("PayPalWebhook.NotFound", "PayPalWebhook not found", ErrorType.NotFound);
}