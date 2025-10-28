using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Rehi.Application.Abstraction.Data;
using Rehi.Application.Abstraction.Messaging;
using Rehi.Application.Abstraction.Paypal;
using Rehi.Domain.Common;
using Rehi.Domain.Subscription;
using Rehi.Domain.Users;
using Rehi.Domain.Webhook;

namespace Rehi.Application.Subscriptions.PayPalWebhook;

public abstract class ReceivePayPalWebhook
{
    public record Command(
        string Payload,
        string TransmissionId,
        string TransmissionTime,
        string TransmissionSig,
        string CertUrl
    ) : ICommand<Response>;

    public record Response(string Status);

    internal class Handler(
        IDbContext dbContext,
        ILogger<ReceivePayPalWebhook> logger,
        IPayPalWebHookService payPalWebHookService
    ) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var result = await payPalWebHookService.ReceivePayPalWebhook(
                request.Payload,
                request.TransmissionId,
                request.TransmissionTime,
                request.TransmissionSig,
                request.CertUrl
            );

            if (result.Status != "success")
            {
                logger.LogWarning("PayPal webhook verification failed. Status: {Status}", result.Status);
                return Result.Failure<Response>(PayPalWebhookErrors.NotFound);
            }

            await ProcessEventAsync(result.EventType, result.PaypalSubscriptionId, cancellationToken);
            
            return new Response("success");
        }

        private async Task ProcessEventAsync(string eventType, string subscriptionId, CancellationToken cancellationToken)
        {
            var subscription = await dbContext.UserSubscriptions
                .Include(s => s.SubscriptionPlan)
                .SingleOrDefaultAsync(us => us.PayPalSubscriptionId == subscriptionId, cancellationToken);

            if (subscription is null)
            {
                logger.LogWarning("Subscription not found for PayPal ID: {Id}", subscriptionId);
                return;
            }

            var handled = eventType switch
            {
                "BILLING.SUBSCRIPTION.ACTIVATED" => HandleActivated(subscription),
                "BILLING.SUBSCRIPTION.CANCELLED" => HandleCancelled(subscription),
                "BILLING.SUBSCRIPTION.SUSPENDED" => HandleSuspended(subscription),
                "PAYMENT.SALE.COMPLETED" => HandlePaymentCompleted(subscription),
                _ => false
            };

            if (handled)
            {
                await dbContext.SaveChangesAsync(cancellationToken);
                logger.LogInformation("Processed {EventType} for subscription: {Id}", eventType, subscriptionId);
            }
            else
            {
                logger.LogInformation("Ignored event type: {EventType}", eventType);
            }
        }

        private bool HandleActivated(UserSubscription subscription)
        {
            subscription.Status = SubscriptionStatus.Active;
            subscription.AutoRenew = true;
            subscription.CurrentPeriodEnd = CalculateNextPeriodEnd(subscription);
            return true;
        }

        private bool HandleCancelled(UserSubscription subscription)
        {
            subscription.Status = SubscriptionStatus.Cancelled;
            subscription.CancelledAt = DateTime.UtcNow;
            subscription.AutoRenew = false;
            return true;
        }

        private bool HandleSuspended(UserSubscription subscription)
        {
            subscription.Status = SubscriptionStatus.Suspended;
            subscription.AutoRenew = false;
            return true;
        }

        private bool HandlePaymentCompleted(UserSubscription subscription)
        {
            subscription.Status = SubscriptionStatus.Active;
            subscription.CurrentPeriodEnd = CalculateNextPeriodEnd(subscription);
            subscription.AutoRenew = true;
            return true;
        }

        private DateTime CalculateNextPeriodEnd(UserSubscription subscription)
        {
            var durationDays = subscription.SubscriptionPlan?.DurationDays ?? 30;
            return DateTime.UtcNow.AddDays(durationDays);
        }
    }
}