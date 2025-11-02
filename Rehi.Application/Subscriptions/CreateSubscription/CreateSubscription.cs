using Microsoft.EntityFrameworkCore;
using Rehi.Application.Abstraction.Authentication;
using Rehi.Application.Abstraction.Data;
using Rehi.Application.Abstraction.Messaging;
using Rehi.Application.Abstraction.Payments;
using Rehi.Domain.Common;
using Rehi.Domain.Subscription;
using Rehi.Domain.Subscriptions;
using Rehi.Domain.Users;

namespace Rehi.Application.Subscriptions.CreateSubscription;

public abstract class CreateSubscription
{
    public record Command(Guid SubscriptionId, string Provider) : ICommand<Response>;

    public record Response(string ApprovalUrl, string SubscriptionId, string Provider);

    internal class Handler(
        IDbContext dbContext,
        IUserContext userContext,
        IPaymentFactory paymentFactory
    ) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var user = await dbContext.Users
                .SingleOrDefaultAsync(u => u.Email == userContext.Email, cancellationToken);

            if (user is null)
                return Result.Failure<Response>(UserErrors.NotFound);

            var plan = await dbContext.SubscriptionPlans
                .SingleOrDefaultAsync(sp => sp.Id == request.SubscriptionId, cancellationToken);

            if (plan is null)
                return Result.Failure<Response>(SubscriptionErrors.NotFound);

            var hasActiveSubscription = await dbContext.UserSubscriptions
                .AnyAsync(s =>
                        s.UserId == user.Id &&
                        (s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.Pending),
                    cancellationToken);

            if (hasActiveSubscription)
                return Result.Failure<Response>(SubscriptionErrors.AlreadyExists);

            var paymentService = paymentFactory.Create(request.Provider);
            var paypalResult = await paymentService.CreateSubscriptionAsync(plan.PaypalPlanId!);

            if (!paypalResult.Success)
                return Result.Failure<Response>(SubscriptionErrors.FailedToCreate);

            var subscription = new UserSubscription
            {
                UserId = user.Id,
                SubscriptionPlanId = request.SubscriptionId,
                PaymentProvider = request.Provider,
                PayPalSubscriptionId = paypalResult.SubscriptionId,
                Status = SubscriptionStatus.Pending,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(plan.DurationDays),
                CurrentPeriodEnd = DateTime.UtcNow.AddDays(plan.DurationDays),
                AutoRenew = true
            };

            dbContext.UserSubscriptions.Add(subscription);
            await dbContext.SaveChangesAsync(cancellationToken);

            return new Response(
                paypalResult.ApprovalUrl,
                paypalResult.SubscriptionId,
                request.Provider
            );
        }
    }
}