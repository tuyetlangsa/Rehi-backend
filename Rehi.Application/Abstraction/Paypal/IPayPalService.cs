namespace Rehi.Application.Abstraction.Paypal;

public interface IPayPalService
{
    Task<PayPalSubscriptionResponse> CreateSubscriptionAsync(string planId);
    Task<PayPalSubscriptionDetails> GetSubscriptionAsync(string subscriptionId);
    Task CancelSubscriptionAsync(string subscriptionId);
}