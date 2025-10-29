namespace Rehi.Domain.Webhook;

public record PayPalWebhookResponse(string Status, string EventType = "None", string PaypalSubscriptionId = "");
