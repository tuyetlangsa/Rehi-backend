using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Rehi.Application.Abstraction.Paypal;
using Rehi.Domain.Subscription;
using Rehi.Domain.Webhook;

namespace Rehi.Infrastructure.Payment.Paypal;

public class PayPalWebhookService(
    ILogger<PayPalWebhookService> logger,
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration) : IPayPalWebHookService
{
    private readonly ILogger<PayPalWebhookService> _logger = logger;
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly PayPalSettings _payPalSettings =
        configuration.GetSection("PayPalSettings").Get<PayPalSettings>()
        ?? throw new ArgumentNullException(nameof(PayPalSettings));
    
    public async Task<PayPalWebhookResponse> ReceivePayPalWebhook(
        string payload,
        string transmissionId,
        string transmissionTime,
        string transmissionSig,
        string certUrl)
    {
        try
        {
            string webHookId = _payPalSettings.WebhookId;
            uint crc = ComputeCrc32(payload);
            string message = $"{transmissionId}|{transmissionTime}|{webHookId}|{crc}";

            bool isValid = await VerifySignatureAsync(certUrl, transmissionSig, message);
            if (!isValid)
            {
                _logger.LogWarning("Invalid PayPal signature - webhook ignored");
                return new PayPalWebhookResponse("Invalid signature");
            }

            var json = JObject.Parse(payload);
            string? eventType = json["event_type"]?.ToString();
            string? subscriptionId = json["resource"]?["id"]?.ToString();

            if (string.IsNullOrEmpty(eventType) || string.IsNullOrEmpty(subscriptionId))
            {
                _logger.LogWarning("Missing event_type or subscription id in webhook payload");
                return new PayPalWebhookResponse("Invalid");
            }

            _logger.LogInformation("Received PayPal event {EventType} for subscription {SubscriptionId}", eventType, subscriptionId);
            
            return new PayPalWebhookResponse("Success", eventType,subscriptionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing PayPal webhook");
            return new PayPalWebhookResponse("Error");
        }
    }

    private async Task<bool> VerifySignatureAsync(string certUrl, string transmissionSig, string message)
    {
        try
        {
            var certPem = await DownloadAndCacheAsync(certUrl);
            var cert = new X509Certificate2(Encoding.UTF8.GetBytes(certPem));
            using var rsa = cert.GetRSAPublicKey();

            byte[] sigBytes = Convert.FromBase64String(transmissionSig);
            byte[] messageBytes = Encoding.UTF8.GetBytes(message);

            bool verified = rsa!.VerifyData(messageBytes, sigBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            return verified;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying PayPal signature");
            return false;
        }
    }

    private async Task<string> DownloadAndCacheAsync(string url)
    {
        string cacheDir = Path.Combine(Path.GetTempPath(), "paypal-cert-cache");
        Directory.CreateDirectory(cacheDir);

        string cacheKey = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(url)))
            .Replace("/", "_").Replace("+", "-");
        string filePath = Path.Combine(cacheDir, $"{cacheKey}.pem");

        if (File.Exists(filePath))
        {
            var lastWrite = File.GetLastWriteTimeUtc(filePath);
            if ((DateTime.UtcNow - lastWrite).TotalHours < 24)
            {
                return await File.ReadAllTextAsync(filePath);
            }
        }

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue { NoCache = true };
        var certPem = await client.GetStringAsync(url);

        await File.WriteAllTextAsync(filePath, certPem);
        _logger.LogInformation("Cached PayPal certificate to {Path}", filePath);

        return certPem;
    }

    private static uint ComputeCrc32(string input)
    {
        var table = Enumerable.Range(0, 256).Select(i =>
        {
            uint crc = (uint)i;
            for (int j = 0; j < 8; j++)
                crc = (crc >> 1) ^ (0xEDB88320u & ~((crc & 1u) - 1u));
            return crc;
        }).ToArray();

        uint hash = 0xFFFFFFFFu;
        foreach (var b in Encoding.UTF8.GetBytes(input))
            hash = (hash >> 8) ^ table[(hash ^ b) & 0xFF];
        return ~hash;
    }

    private Task HandleSubscriptionActivatedAsync(string subscriptionId)
    {
        _logger.LogInformation("✅ Subscription {SubscriptionId} activated", subscriptionId);
        return Task.CompletedTask;
    }

    private Task HandleSubscriptionCancelledAsync(string subscriptionId)
    {
        _logger.LogInformation("🚫 Subscription {SubscriptionId} cancelled", subscriptionId);
        return Task.CompletedTask;
    }

    private Task HandleSubscriptionSuspendedAsync(string subscriptionId)
    {
        _logger.LogInformation("⏸️ Subscription {SubscriptionId} suspended", subscriptionId);
        return Task.CompletedTask;
    }
}
