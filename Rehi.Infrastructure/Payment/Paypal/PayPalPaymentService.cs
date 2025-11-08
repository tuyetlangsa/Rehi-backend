using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using Rehi.Application.Abstraction.Data;
using Rehi.Application.Abstraction.Payments;
using Rehi.Domain.Payment;
using Rehi.Domain.Subscription;
using Rehi.Infrastructure.Payment.PayOS;

namespace Rehi.Infrastructure.Paypal;

public class PayPalPaymentService : IPaymentService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PayPalPaymentService> _logger;
    private readonly PayPalSettings _settings;
    private readonly IDbContext _dbContext;
    private readonly IOptions<SubscriptionOptions> _options;
    public PayPalPaymentService(HttpClient httpClient, ILogger<PayPalPaymentService> logger,
        IConfiguration configuration, IDbContext dbContext, IOptions<SubscriptionOptions> options)
    {
        _httpClient = httpClient;
        _logger = logger;
        _settings = configuration.GetSection("PayPalSettings").Get<PayPalSettings>() ??
                    throw new ArgumentNullException(nameof(PayPalSettings));
        _dbContext = dbContext;
        _options = options;
    }

    public async Task<PaymentCreateResult> CreateSubscriptionAsync(Guid planId)
    {
        var token = await GetAccessTokenAsync();
        var plan = await _dbContext.SubscriptionPlans.SingleOrDefaultAsync(s => s.Id == planId);
        if (plan is null)
        {
            return new PaymentCreateResult
            {
                Success = false,
                Message = "Plan not found"
            };
        }
        var payload = new PayPalSubscriptionCreateRequest
        {
            plan_id = plan.PaypalPlanId!,
            application_context = new PayPalApplicationContext
            {
                return_url = _options.Value.Success,
                cancel_url = _options.Value.Cancel,
            }
        };
        var json = JsonSerializer.Serialize(payload);
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_settings.BaseUrl}/v1/billing/subscriptions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadAsStringAsync();
        return MapToPaymentResult(result);
    }

    public async Task<PaymentCancelResult> CancelSubscriptionAsync(PaymentCancelRequest cancelRequest)
    {
        var token = await GetAccessTokenAsync();

        _logger.LogInformation("Sending PATCH request to cancel/update PayPal subscription {SubscriptionId}",
            cancelRequest.SubscriptionId);

        using var request = new HttpRequestMessage(HttpMethod.Post,
            $"{_settings.BaseUrl}/v1/billing/subscriptions/{cancelRequest.SubscriptionId}/cancel");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var postPayload = new JObject
        {
            ["reason"] = cancelRequest.Reason
        };


        var jsonPayload = postPayload.ToString();
        request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        _logger.LogDebug(" Post Payload: {Payload}", jsonPayload);

        var response = await _httpClient.SendAsync(request);
        var responseContent = await response.Content.ReadAsStringAsync();

        _logger.LogDebug("📨 PayPal Response: {StatusCode} - {Content}", response.StatusCode, responseContent);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Failed to update subscription {SubscriptionId}. Status: {StatusCode}, Response: {Response}",
                cancelRequest.SubscriptionId, response.StatusCode, responseContent);
            throw new Exception($"Failed to update subscription: {response.StatusCode}, {responseContent}");
        }

        _logger.LogInformation("Successfully updated PayPal subscription {SubscriptionId}",
            cancelRequest.SubscriptionId);

        return new PaymentCancelResult
        {
            Success = true,
            Message = "Subscription updated successfully"
        };
    }

    private async Task<string> GetAccessTokenAsync()
    {
        _httpClient.DefaultRequestHeaders.Clear();
        var authHeader =
            Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_settings.ClientId}:{_settings.ClientSecret}"));
        var request = new HttpRequestMessage(HttpMethod.Post, $"{_settings.BaseUrl}/v1/oauth2/token");
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authHeader);
        request.Content = new StringContent("grant_type=client_credentials", Encoding.UTF8,
            "application/x-www-form-urlencoded");

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadAsStringAsync();
        var token = JsonSerializer.Deserialize<PayPalTokenResponse>(result) ??
                    throw new Exception("Failed to parse token");

        return token.access_token;
    }

    private PaymentCreateResult MapToPaymentResult(string json)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        var response = JsonSerializer.Deserialize<PayPalSubscriptionResponse>(json, options);

        if (response is null)
            return new PaymentCreateResult
            {
                Success = false,
                Message = "Failed to parse PayPal response"
            };

        var approvalUrl = response.links
            ?.FirstOrDefault(l => l.rel == "approve")
            ?.href ?? string.Empty;

        return new PaymentCreateResult
        {
            Success = true,
            Message = "Subscription created successfully",
            SubscriptionId = response.id,
            ApprovalUrl = approvalUrl,
            Status = response.status,
            Provider = "PayPal"
        };
    }

    public async Task<PayPalSubscriptionDetails> GetSubscriptionAsync(string subscriptionId)
    {
        var token = await GetAccessTokenAsync();
        using var request = new HttpRequestMessage(HttpMethod.Get,
            $"{_settings.BaseUrl}/v1/billing/subscriptions/{subscriptionId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<PayPalSubscriptionDetails>(result);
    }
}