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
        IHttpClientFactory httpClientFactory,
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

            var eventType = result.EventType;
            var subscriptionId = result.PaypalSubscriptionId;

            // Handle event types
            switch (eventType)
            {
                case "BILLING.SUBSCRIPTION.ACTIVATED":
                    await HandleSubscriptionActivatedAsync(subscriptionId);
                    break;

                case "BILLING.SUBSCRIPTION.CANCELLED":
                    await HandleSubscriptionCancelledAsync(subscriptionId);
                    break;

                case "BILLING.SUBSCRIPTION.SUSPENDED":
                    await HandleSubscriptionSuspendedAsync(subscriptionId);
                    break;

                default:
                    logger.LogInformation("Ignored event type: {EventType}", eventType);
                    break;
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            return new Response("success");
        }

        private async Task HandleSubscriptionActivatedAsync(string subscriptionId)
        {
            var subscription = await dbContext.UserSubscriptions
                .SingleOrDefaultAsync(us => us.PayPalSubscriptionId == subscriptionId);

            if (subscription is null)
            {
                logger.LogWarning("Subscription not found for PayPal ID: {Id}", subscriptionId);
                return;
            }

            subscription.Status = SubscriptionStatus.Active;
            logger.LogInformation("Activated subscription: {Id}", subscriptionId);
        }

        private async Task HandleSubscriptionCancelledAsync(string subscriptionId)
        {
            var subscription = await dbContext.UserSubscriptions
                .SingleOrDefaultAsync(us => us.PayPalSubscriptionId == subscriptionId);

            if (subscription is null)
            {
                logger.LogWarning("Subscription not found for PayPal ID: {Id}", subscriptionId);
                return;
            }

            subscription.Status = SubscriptionStatus.Cancelled;
            logger.LogInformation("Cancelled subscription: {Id}", subscriptionId);
        }

        private async Task HandleSubscriptionSuspendedAsync(string subscriptionId)
        {
            var subscription = await dbContext.UserSubscriptions
                .SingleOrDefaultAsync(us => us.PayPalSubscriptionId == subscriptionId);

            if (subscription is null)
            {
                logger.LogWarning("Subscription not found for PayPal ID: {Id}", subscriptionId);
                return;
            }

            subscription.Status = SubscriptionStatus.Cancelled;
            logger.LogInformation("Suspended subscription: {Id}", subscriptionId);
        }
    }
}
