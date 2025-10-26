using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Rehi.Application.Abstraction.Authentication;
using Rehi.Application.Abstraction.Paypal;
using Rehi.Domain.Subscription;

namespace Rehi.Infrastructure.Paypal;

public class PayPalService : IPayPalService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PayPalService> _logger;
    private readonly PayPalSettings settings;

    public PayPalService(HttpClient httpClient, ILogger<PayPalService> logger, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        settings = configuration.GetSection("PayPalSettings").Get<PayPalSettings>() ?? throw new ArgumentNullException(nameof(PayPalSettings));
    }

    private async Task<string> GetAccessTokenAsync()
    {
        var authHeader =
            Convert.ToBase64String(Encoding.UTF8.GetBytes($"{settings.ClientId} : {settings.ClientSecret}"));
        var request = new HttpRequestMessage(HttpMethod.Post, $"{settings.BaseUrl}/v1/oauth2/token");
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authHeader);
        request.Content = new StringContent("grant_type=client_credentials", Encoding.UTF8, "application/x-www-form-urlencoded");

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadAsStringAsync();
        var token = JsonSerializer.Deserialize<PayPalTokenResponse>(result);

        return token.access_token;
    }

    public async Task<PayPalSubscriptionResponse> CreateSubscriptionAsync(string planId)
    {
        var token = await GetAccessTokenAsync();

        var payload = new PayPalSubscriptionCreateRequest
        {
            plan_id = planId,
            application_context = new PayPalApplicationContext
            {
                return_url = "https://your-domain.com/api/paypal/success",
                cancel_url = "https://your-domain.com/api/paypal/cancel"
            }
        };
        var json = JsonSerializer.Serialize(payload);
        var request = new HttpRequestMessage(HttpMethod.Post, $"{settings.BaseUrl}/v1/billing/subscriptions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<PayPalSubscriptionResponse>(result);
    }
    
    public async Task<PayPalSubscriptionDetails> GetSubscriptionAsync(string subscriptionId)
    {
        var token = await GetAccessTokenAsync();
        var request = new HttpRequestMessage(HttpMethod.Get, $"{settings.BaseUrl}/v1/billing/subscriptions/{subscriptionId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<PayPalSubscriptionDetails>(result);
    }

    public async Task CancelSubscriptionAsync(string subscriptionId)
    {
        var token = await GetAccessTokenAsync();

        var request = new HttpRequestMessage(HttpMethod.Post, $"{settings.BaseUrl}/v1/billing/subscriptions/{subscriptionId}/cancel");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = new StringContent("{\"reason\":\"User cancelled manually\"}", Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }
}