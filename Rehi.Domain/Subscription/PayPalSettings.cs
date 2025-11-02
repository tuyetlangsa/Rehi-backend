namespace Rehi.Domain.Subscription;

public class PayPalSettings
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string Mode { get; set; } = "Sandbox";
    public string BaseUrl { get; set; } = "https://api.sandbox.paypal.com";
    public string WebhookId { get; set; } = string.Empty;
}