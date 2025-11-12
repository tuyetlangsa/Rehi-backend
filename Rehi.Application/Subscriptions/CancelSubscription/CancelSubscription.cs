using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Rehi.Application.Abstraction.Authentication;
using Rehi.Application.Abstraction.Data;
using Rehi.Application.Abstraction.Messaging;
using Rehi.Application.Abstraction.Payments;
using Rehi.Domain.Common;
using Rehi.Domain.Payment;
using Rehi.Domain.Subscription;
using Rehi.Domain.Subscriptions;

namespace Rehi.Application.Subscriptions.CancelSubscription;

public class CancelSubscription
{
    public record Command(string Provider) : ICommand<Response>;

    public record Response(bool Success, string Message);

    internal class Handler(
        IDbContext dbContext,
        IUserContext userContext,
        IPaymentFactory paymentFactory,
        ILogger<CancelSubscription> logger
    ) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var subscription = await dbContext.UserSubscriptions
                .Include(us => us.User)
                .Where(us =>
                    us.User.Email == userContext.Email &&
                    (us.Status == SubscriptionStatus.Active || us.Status == SubscriptionStatus.Pending))
                .FirstOrDefaultAsync(cancellationToken);

            if (subscription is null)
                return Result.Failure<Response>(SubscriptionErrors.NotFound);

            if (string.IsNullOrEmpty(subscription.ExternalSubscriptionId))
            {
                logger.LogWarning("External subscription ID is missing for subscription {Id}", subscription.Id);
                return Result.Failure<Response>(SubscriptionErrors.InvalidSubscription);
            }

            var paymentService = paymentFactory.Create(request.Provider);
            var cancelRequest = new PaymentCancelRequest
            {
                SubscriptionId = subscription.ExternalSubscriptionId,
                Reason = "User requested cancellation"
            };

            try
            {
                var result = await paymentService.CancelSubscriptionAsync(cancelRequest);

                if (!result.Success)
                {
                    logger.LogWarning("Payment provider failed to cancel subscription {Id}", subscription.Id);
                    return Result.Failure<Response>(SubscriptionErrors.FailedToCancel);
                }

                logger.LogInformation(
                    "Cancellation request sent to PayPal for subscription {Id}. Waiting for webhook confirmation.",
                    subscription.Id);

                return Result.Success(new Response(true,
                    $"Subscription cancelled. You still have access until {subscription.CurrentPeriodEnd:yyyy-MM-dd}"));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to cancel subscription {Id}", subscription.Id);
                return Result.Failure<Response>(SubscriptionErrors.FailedToCancel);
            }
        }
    }
}