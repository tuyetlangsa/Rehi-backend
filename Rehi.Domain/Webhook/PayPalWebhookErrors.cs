using Rehi.Domain.Common;

namespace Rehi.Domain.Webhook;

public class PayPalWebhookErrors
{
    public static Error NotFound => new("PayPalWebhook.NotFound", "PayPalWebhook not found", ErrorType.NotFound);

    public static Error InvalidSignature =>
        new("PayPalWebhook.InvalidSignature", "Invalid Signature", ErrorType.Failure);

    public static Error InvalidPayload => new("PayPalWebhook.InvalidPayload", "Invalid Payload", ErrorType.Failure);

    public static Error InvalidEventType =>
        new("PayPalWebhook.InvalidEventType", "Invalid Event Type", ErrorType.Failure);
}