using Microsoft.Extensions.DependencyInjection;
using Rehi.Application.Abstraction.Payments;
using Rehi.Infrastructure.Payment.PayOS;
using Rehi.Infrastructure.Paypal;

namespace Rehi.Infrastructure.Payment;

public class PaymentFactory : IPaymentFactory
{
    private readonly IServiceProvider _serviceProvider;

    public PaymentFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IPaymentService Create(string provider)
    {
        return provider.ToLower() switch
        {
            "paypal" => _serviceProvider.GetRequiredService<PayPalPaymentService>(),
            "payos" => _serviceProvider.GetRequiredService<PayOsPaymentService>(),
            //add more providers here
            _ => throw new NotSupportedException($"Payment provider '{provider}' is not supported.")
        };
    }
}