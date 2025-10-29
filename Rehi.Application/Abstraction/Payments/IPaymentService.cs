using Rehi.Domain.Payment;

namespace Rehi.Application.Abstraction.Payments;

public interface IPaymentService
{
    Task<PaymentCreateResult> CreateSubscriptionAsync(string planId);
    Task<PaymentCancelResult> CancelSubscriptionAsync(PaymentCancelRequest request);
}