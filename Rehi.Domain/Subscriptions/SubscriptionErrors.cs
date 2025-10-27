using Rehi.Domain.Common;

namespace Rehi.Domain.Subscriptions;

public static class SubscriptionErrors
{
    public static Error NotFound => new("Subscription.NotFound", "Subscription not found", ErrorType.NotFound);
}