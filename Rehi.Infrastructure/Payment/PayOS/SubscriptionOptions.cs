namespace Rehi.Infrastructure.Payment.PayOS;

public class SubscriptionOptions
{
    public const string Subscription = "Subscription";
    public string Success { get; set; } = string.Empty;
    public string Cancel { get; set; } = string.Empty;
}