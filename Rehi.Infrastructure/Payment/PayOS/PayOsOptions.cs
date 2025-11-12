namespace Rehi.Infrastructure.Payment.PayOS;

public class PayOsOptions
{
    public const string PayOs = "PayOs";
    public string ClientId { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string CheckSum { get; set; } = string.Empty;
}