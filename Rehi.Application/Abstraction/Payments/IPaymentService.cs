using Rehi.Domain.Payment;

namespace Rehi.Application.Abstraction.Payments;

public interface IPaymentService
{
    Task<PaymentResult> CreateSubscriptionAsync(string planId);
}