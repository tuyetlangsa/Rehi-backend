using Rehi.Domain.Common;
using Rehi.Domain.Users;

namespace Rehi.Domain.Subscription;

public class SubscriptionPlan : Entity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int DurationDays { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? PaypalPlanId { get; set; }
    public virtual ICollection<UserSubscription> UserSubscriptions { get; set; } = new List<UserSubscription>();
}