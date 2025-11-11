using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Rehi.Application.Abstraction.Data;
using Rehi.Application.Abstraction.Payments;
using Rehi.Domain.Payment;
using Net.payOS.Types;
using Rehi.Domain.Subscription;

namespace Rehi.Infrastructure.Payment.PayOS;

public class PayOsPaymentService : IPaymentService
{
    private readonly PayOsOptions _payOsConfig;
    private readonly SubscriptionOptions _subscriptionOptions;
    private readonly ILogger<PayOsPaymentService> _logger;
    private readonly IDbContext _dbContext;
    private readonly Net.payOS.PayOS _payOs;
    
    // Constants
    private const int VndExchangeRate = 26000;
    private const int OrderCodeRandomMin = 100;
    private const int OrderCodeRandomMax = 1000;
    private const int TimestampModulo = 1000000;
    
    public PayOsPaymentService(
        ILogger<PayOsPaymentService> logger,
        IOptions<SubscriptionOptions> subscriptionOptions,
        IDbContext dbContext,
        IOptions<PayOsOptions> payOsConfig)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _subscriptionOptions = subscriptionOptions?.Value ?? throw new ArgumentNullException(nameof(subscriptionOptions));
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _payOsConfig = payOsConfig?.Value ?? throw new ArgumentNullException(nameof(payOsConfig));
        
        // Initialize PayOS instance
        _payOs = new Net.payOS.PayOS(
            _payOsConfig.ClientId, 
            _payOsConfig.ApiKey, 
            _payOsConfig.CheckSum
        );
    }

    public async Task<PaymentCreateResult> CreateSubscriptionAsync(Guid planId)
    {
        try
        {
            // Retrieve and validate subscription plan
            var plan = await GetSubscriptionPlanAsync(planId);
            if (plan is null)
            {
                return CreateFailureResult("Subscription plan not found");
            }

            if (!IsValidPrice(plan.Price))
            {
                _logger.LogWarning("Invalid price for plan {PlanId}: {Price}", planId, plan.Price);
                return CreateFailureResult("Invalid subscription plan price");
            }

            // Create payment link
            var paymentResponse = await CreatePaymentLinkAsync(plan);
            
            if (string.IsNullOrWhiteSpace(paymentResponse?.checkoutUrl))
            {
                _logger.LogWarning("Empty checkout URL for plan {PlanId}", planId);
                return CreateFailureResult("Failed to generate payment URL");
            }

            _logger.LogInformation(
                "Payment link created | Plan: {PlanId} | Url: {Url}",
                planId, 
                paymentResponse.checkoutUrl
            );

            return new PaymentCreateResult
            {
                Success = true,
                Message = "Payment link successfully created",
                ApprovalUrl = paymentResponse.checkoutUrl,
                SubscriptionId = paymentResponse.orderCode.ToString(),
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment link for plan {PlanId}", planId);
            return CreateFailureResult("Unable to process payment. Please try again later.");
        }
    }

    public Task<PaymentCancelResult> CancelSubscriptionAsync(PaymentCancelRequest request)
    {
        throw new NotImplementedException("Subscription cancellation is not yet implemented");
    }

    // Private helper methods
    
    private async Task<SubscriptionPlan?> GetSubscriptionPlanAsync(Guid planId)
    {
        var plan = await _dbContext.SubscriptionPlans
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == planId);

        if (plan is null)
        {
            _logger.LogWarning("Subscription plan {PlanId} not found", planId);
        }

        return plan;
    }

    private static bool IsValidPrice(decimal price) => price > 0;

    private async Task<CreatePaymentResult> CreatePaymentLinkAsync(SubscriptionPlan plan)
    {
        var orderCode = GenerateOrderCode();
        var amountVnd = ConvertToVnd(plan.Price);

        var paymentData = BuildPaymentData(plan, orderCode, amountVnd);
        
        return await _payOs.createPaymentLink(paymentData);
    }

    private PaymentData BuildPaymentData(
        SubscriptionPlan plan, 
        long orderCode, 
        int amountVnd)
    {
        var items = new List<ItemData>
        {
            new ItemData(plan.Name, quantity: 1, price: amountVnd)
        };

        var description = $"Payment Subscription"; // Dynamic description

        return new PaymentData(
            orderCode: orderCode,
            amount: amountVnd,
            description: description,
            items: items,
            cancelUrl: _subscriptionOptions.Cancel ?? "https://localhost:3002",
            returnUrl: _subscriptionOptions.Success ?? "https://localhost:3002"
        );
    }

    private static int ConvertToVnd(decimal usdAmount) => (int)(usdAmount * VndExchangeRate);

    private static long GenerateOrderCode()
    {
        var randomPart = Random.Shared.Next(OrderCodeRandomMin, OrderCodeRandomMax);
        var timestampPart = DateTimeOffset.UtcNow.ToUnixTimeSeconds() % TimestampModulo;
        return timestampPart * OrderCodeRandomMax + randomPart; // Better distribution
    }

    private static PaymentCreateResult CreateFailureResult(string message)
    {
        return new PaymentCreateResult
        {
            Success = false,
            Message = message
        };
    }
}