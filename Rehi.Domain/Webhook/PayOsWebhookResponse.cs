namespace Rehi.Domain.Webhook;

public record PayOsWebhookResponse(string Code, string Desc, bool Success);