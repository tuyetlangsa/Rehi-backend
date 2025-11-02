using Rehi.Domain.Common;
using Rehi.Domain.Subscription;

namespace Rehi.Domain.Users;

public class UserSubscription : Entity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid SubscriptionPlanId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public DateTime? CancelledAt { get; set; }
    public DateTime CurrentPeriodEnd { get; set; }
    public bool AutoRenew { get; set; } = true;

    public bool IsActive => DateTime.UtcNow <= CurrentPeriodEnd;

    public User User { get; set; } = null!;
    public SubscriptionPlan SubscriptionPlan { get; set; } = null!;
    public string PaymentProvider { get; set; } = null!;
    public SubscriptionStatus Status { get; set; }
    public string PayPalSubscriptionId { get; set; } = null!;
}