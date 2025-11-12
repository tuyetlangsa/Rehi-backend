using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
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
        Stream Body
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
            string webhookPayload;

            using (var reader = new StreamReader(request.Body))
            {
                webhookPayload = await reader.ReadToEndAsync(cancellationToken);
            }

            if (string.IsNullOrEmpty(webhookPayload))
            {
                logger.LogWarning("PayPal webhook received an empty payload.");
                return Result.Failure<Response>(PayPalWebhookErrors.InvalidPayload);
            }

            logger.LogInformation("PayPal webhook received. Payload: {Payload}", webhookPayload);

            JObject json;
            try
            {
                json = JObject.Parse(webhookPayload);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Invalid JSON format in PayPal webhook payload");
                return Result.Failure<Response>(PayPalWebhookErrors.InvalidPayload);
            }

            var eventType = json["event_type"]?.ToString();

            var subscriptionId =
                json["resource"]?["id"]?.ToString()
                ?? json["resource"]?["billing_agreement_id"]?.ToString()
                ?? json["resource"]?["subscription_id"]?.ToString();

            if (string.IsNullOrEmpty(eventType) || string.IsNullOrEmpty(subscriptionId))
            {
                logger.LogWarning("Missing event_type or subscription_id in webhook payload");
                return Result.Failure<Response>(PayPalWebhookErrors.InvalidEventType);
            }

            logger.LogInformation("📦 PayPal Webhook Event: {EventType}, SubscriptionId: {SubscriptionId}", eventType,
                subscriptionId);

            await ProcessEventAsync(eventType, subscriptionId, cancellationToken);

            return new Response("success");
        }

        private async Task ProcessEventAsync(string eventType, string subscriptionId,
            CancellationToken cancellationToken)
        {
            var subscription = await dbContext.UserSubscriptions
                .Include(s => s.SubscriptionPlan)
                .SingleOrDefaultAsync(us => us.ExternalSubscriptionId == subscriptionId, cancellationToken);

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