using Rehi.Domain.Payment;

namespace Rehi.Application.Abstraction.Payments;

public interface IPaymentService
{
    Task<PaymentCreateResult> CreateSubscriptionAsync(Guid planId);
    Task<PaymentCancelResult> CancelSubscriptionAsync(PaymentCancelRequest request);
}