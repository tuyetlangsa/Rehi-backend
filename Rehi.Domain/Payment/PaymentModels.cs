namespace Rehi.Domain.Payment;

public class PaymentCreateResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string SubscriptionId { get; set; }
    public string ApprovalUrl { get; set; }
    public string Status { get; set; }
    public string Provider { get; set; }
}

public class PaymentCancelRequest
{
    public string SubscriptionId { get; set; }
    public string Reason { get; set; } = null!;
}

public class PaymentCancelResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}