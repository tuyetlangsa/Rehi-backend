using Rehi.Domain.Common;

namespace Rehi.Domain.Subscriptions;

public static class SubscriptionErrors
{
    public static Error AlreadyExists => new("Subscription.AlreadyExists", "Subscription already exists", ErrorType.Conflict);
    public static Error NotFound => new("Subscription.NotFound", "Subscription not found", ErrorType.NotFound);
    public static Error FailedToCancel => new("Subscription.FailedToCancel", "Failed To Cancel", ErrorType.Conflict);
    public static Error InvalidSubscription => new("Subscription.InvalidSubscription", "Invalid Subscription", ErrorType.Conflict);
    public static Error FailedToCreate => new("Subscription.FailedToCreate", "Failed To Create", ErrorType.Conflict);
}