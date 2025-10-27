namespace Rehi.Domain.Payment;

public class PaymentResult
{
    public string SubscriptionId { get; set; }
    public string ApprovalUrl { get; set; }     
    public string Status { get; set; }           
    public string Provider { get; set; }         
}