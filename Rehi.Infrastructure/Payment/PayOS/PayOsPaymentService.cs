using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PayOS;
using PayOS.Exceptions;
using PayOS.Models.V2.PaymentRequests;
using Rehi.Application.Abstraction.Data;
using Rehi.Application.Abstraction.Payments;
using Rehi.Domain.Payment;

namespace Rehi.Infrastructure.Payment.PayOS;

public class PayOsPaymentService : IPaymentService
{
    private readonly PayOsOptions _payOsConfig;
    private readonly SubscriptionOptions _subscriptionOptions;
    private readonly ILogger<PayOsPaymentService> _logger;
    private readonly PayOSClient _client;
    private readonly IDbContext _dbContext;
    
    // Constants for better maintainability
    private const int VndExchangeRate = 26000;
    private const string PaymentDescription = "phong"; // TODO: Make this dynamic based on plan
    
    public PayOsPaymentService(
        ILogger<PayOsPaymentService> logger,
        IOptions<SubscriptionOptions> subscriptionOptions,
        IDbContext dbContext,
        IOptions<PayOsOptions> payOsConfig, 
        [FromKeyedServices("PayOsClient")] PayOSClient client)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _subscriptionOptions = subscriptionOptions?.Value ?? throw new ArgumentNullException(nameof(subscriptionOptions));
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _payOsConfig = payOsConfig?.Value ?? throw new ArgumentNullException(nameof(payOsConfig));
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    public async Task<PaymentCreateResult> CreateSubscriptionAsync(Guid planId)
    {
        // Retrieve plan with cancellation token support
        var plan = await _dbContext.SubscriptionPlans
            .AsNoTracking() // No tracking needed for read-only operation
            .SingleOrDefaultAsync(s => s.Id == planId);

        if (plan is null)
        {
            _logger.LogWarning("Subscription plan {PlanId} not found", planId);
            return CreateFailureResult("Subscription plan not found");
        }

        // Validate plan price
        if (plan.Price <= 0)
        {
            _logger.LogWarning("Invalid price for plan {PlanId}: {Price}", planId, plan.Price);
            return CreateFailureResult("Invalid subscription plan price");
        }

        // Create payment request
        var orderCode = GenerateOrderCode();
        var amount = CalculateAmount(plan.Price);
        var paymentRequest = CreatePaymentRequest(orderCode, amount);

        try
        {
            var paymentResponse = await _client.PaymentRequests.CreateAsync(paymentRequest);

            if (string.IsNullOrEmpty(paymentResponse?.CheckoutUrl))
            {
                _logger.LogWarning("PayOS returned empty checkout URL for plan {PlanId}", planId);
                return CreateFailureResult("Failed to generate payment URL");
            }
            
            _logger.LogInformation(
                "Payment link created successfully for plan {PlanId}, order {OrderCode}", 
                planId, 
                orderCode);

            return new PaymentCreateResult
            {
                Success = true,
                Message = "Payment link successfully created",
                ApprovalUrl = paymentResponse.CheckoutUrl
            };
        }
        catch (PayOSException ex)
        {
            _logger.LogError(
                ex, 
                "PayOS API error when creating subscription payment for plan {PlanId}: {ErrorCode}", 
                planId, 
                ex.Message);

            return CreateFailureResult($"Payment service error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex, 
                "Unexpected error when creating subscription payment for plan {PlanId}", 
                planId);

            return CreateFailureResult("An unexpected error occurred. Please try again later.");
        }
    }

    public Task<PaymentCancelResult> CancelSubscriptionAsync(PaymentCancelRequest request)
    {
        throw new NotImplementedException("Subscription cancellation is not yet implemented");
    }

    // Helper methods for better code organization
    private static long GenerateOrderCode() => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    private static int CalculateAmount(decimal price) => (int)(price * VndExchangeRate);

    private CreatePaymentLinkRequest CreatePaymentRequest(long orderCode, int amount)
    {
        var cancelUrl = _subscriptionOptions.Cancel;
        var returnUrl = _subscriptionOptions.Success;
        var description = PaymentDescription;

        var signature = GeneratePayOsSignature(
            orderCode, 
            amount, 
            cancelUrl, 
            returnUrl, 
            description);

        return new CreatePaymentLinkRequest
        {
            OrderCode = orderCode,
            Amount = amount,
            Description = description,
            CancelUrl = cancelUrl,
            ReturnUrl = returnUrl,
            Signature = signature
        };
    }

    private string GeneratePayOsSignature(
        long orderCode, 
        int amount, 
        string cancelUrl, 
        string returnUrl, 
        string description)
    {
        // Build sorted parameter string for signature
        var sortedData = $"amount={amount}&cancelUrl={cancelUrl}&description={description}&orderCode={orderCode}&returnUrl={returnUrl}";
        
        var keyBytes = Encoding.UTF8.GetBytes(_payOsConfig.CheckSum);
        var dataBytes = Encoding.UTF8.GetBytes(sortedData);

        using var hmac = new HMACSHA256(keyBytes);
        var hashBytes = hmac.ComputeHash(dataBytes);

        return Convert.ToHexString(hashBytes).ToLower();
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