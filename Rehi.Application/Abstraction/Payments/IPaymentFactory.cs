namespace Rehi.Application.Abstraction.Payments;

public interface IPaymentFactory
{
    IPaymentService Create(string provider);
}