using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Net.payOS.Types;
using Newtonsoft.Json;
using Rehi.Application.Abstraction.Data;
using Rehi.Application.Abstraction.PayOs;
using Rehi.Domain.Common;
using Rehi.Domain.Subscription;
using Rehi.Domain.Users;
using Rehi.Domain.Webhook;

namespace Rehi.Infrastructure.Payment.PayOS;

public class PayOsWebhookService : IPayOsWebhookService
{
    private readonly PayOsOptions _payOsConfig;
    private readonly SubscriptionOptions _subscriptionOptions;
    private readonly ILogger<PayOsPaymentService> _logger;
    private readonly IDbContext _dbContext;
    private readonly Net.payOS.PayOS _payOs;

    public PayOsWebhookService(
        ILogger<PayOsPaymentService> logger,
        IOptions<SubscriptionOptions> subscriptionOptions,
        IDbContext dbContext,
        IOptions<PayOsOptions> payOsConfig)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _subscriptionOptions =
            subscriptionOptions?.Value ?? throw new ArgumentNullException(nameof(subscriptionOptions));
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _payOsConfig = payOsConfig?.Value ?? throw new ArgumentNullException(nameof(payOsConfig));

        // Initialize PayOS instance
        _payOs = new Net.payOS.PayOS(
            _payOsConfig.ClientId,
            _payOsConfig.ApiKey,
            _payOsConfig.CheckSum
        );
    }

    public async Task<PayOsWebhookResponse> ReceivePayOsWebhook(Stream rawBody)
    {
        try
        {
            using var reader = new StreamReader(rawBody);
            var bodyString = await reader.ReadToEndAsync();

            _logger.LogInformation("Received PayOS webhook: {Body}", bodyString);

            var webhookObject = JsonConvert.DeserializeObject<WebhookType>(bodyString);

            // ✅ Step 1: Verify webhook with PayOS SDK
            var webhookData = _payOs.verifyPaymentWebhookData(webhookObject);

            _logger.LogInformation("✅ Verified PayOS webhook for OrderCode {OrderCode}, Code {Code}",
                webhookData.orderCode, webhookData.code);

            // ✅ Step 2: Process payment
            await ProcessPaymentAsync(webhookData.orderCode, webhookData.code, webhookData.amount, CancellationToken.None);

            return new PayOsWebhookResponse(
                webhookData.code,
                webhookData.desc,
                webhookData.desc == "success"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error verifying or processing PayOS webhook");
            return new PayOsWebhookResponse("99", ex.Message, false);
        }
    }


    private async Task ProcessPaymentAsync(long orderCode, string payOsCode, long amount, CancellationToken cancellationToken)
    {
        var subscription = await _dbContext.UserSubscriptions
            .Include(s => s.SubscriptionPlan)
            .SingleOrDefaultAsync(s => s.ExternalSubscriptionId == orderCode.ToString(), cancellationToken);

        if (subscription is null)
        {
            _logger.LogWarning("⚠️ Subscription not found for OrderCode: {OrderCode}", orderCode);
            return;
        }

        switch (payOsCode)
        {
            case "00": // ✅ Payment success
                subscription.Status = SubscriptionStatus.Active;
                subscription.CurrentPeriodEnd = CalculateNextPeriodEnd(subscription);
                subscription.AutoRenew = true;
                _logger.LogInformation("✅ Subscription {Id} marked as Active", subscription.Id);
                break;

            case "09": // ❌ Payment failed
                subscription.Status = SubscriptionStatus.Cancelled;
                subscription.AutoRenew = false;
                _logger.LogWarning("❌ Subscription {Id} payment failed", subscription.Id);
                break;

            default:
                _logger.LogInformation("ℹ️ Unhandled PayOS code {Code} for subscription {Id}", payOsCode, subscription.Id);
                break;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }


    private DateTime CalculateNextPeriodEnd(UserSubscription subscription)
    {
        var durationDays = subscription.SubscriptionPlan?.DurationDays ?? 30;
        return DateTime.UtcNow.AddDays(durationDays);
    }
}