using Microsoft.EntityFrameworkCore;
using Rehi.Application.Abstraction.Authentication;
using Rehi.Application.Abstraction.Data;
using Rehi.Application.Abstraction.Messaging;
using Rehi.Domain.Common;
using Rehi.Domain.Subscription;
using Rehi.Domain.Users;

namespace Rehi.Application.Subscriptions.GetSubscriptionPlanByUserEmail;

public record SubscriptionPlanDto(
    Guid Id,
    string Name,
    decimal Price,
    string Description,
    SubscriptionStatus Status,
    DateTime? CurrentPeriodEnd,
    string Provider
);

public class GetSubscriptionPlanByUserEmail
{
    public record Command : ICommand<Response>;

    public record Response(SubscriptionPlanDto Plan);

    internal class Handler(IDbContext dbContext, IUserContext userContext) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var user = await dbContext.Users.SingleOrDefaultAsync(u => u.Email == userContext.Email, cancellationToken);

            if (user is null)
                return Result.Failure<Response>(UserErrors.NotFound);
            var nowUtc = DateTime.UtcNow;

            var subscriptionPlanOfUser = await dbContext.UserSubscriptions
                .Include(us => us.SubscriptionPlan)
                .Where(us =>
                    us.UserId == user.Id && us.Status != SubscriptionStatus.Pending && nowUtc < us.CurrentPeriodEnd)
                .OrderBy(us => us.CurrentPeriodEnd) // optional: pick the earliest ending subscription
                .FirstOrDefaultAsync(cancellationToken);
            
            if (subscriptionPlanOfUser is not null)
            {
                var plan = subscriptionPlanOfUser.SubscriptionPlan;

                var planDto = new SubscriptionPlanDto(
                    plan.Id,
                    plan.Name,
                    plan.Price,
                    plan.Description,
                    subscriptionPlanOfUser.Status,
                    subscriptionPlanOfUser.CurrentPeriodEnd,
                    subscriptionPlanOfUser.PaymentProvider
                );

                return new Response(planDto);
            }
            else
            {
                
            }

            return Result.Failure<Response>(UserErrors.NoActiveSubscription);
        }
    }
}