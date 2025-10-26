using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Rehi.Infrastructure.Paypal;

public static class PayPalHttpClientExtensions
{
    public static IServiceCollection AddPayPalHttpClient(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient<PayPalService>(client =>
        {
            client.BaseAddress = new Uri(configuration["PayPalSettings:BaseUrl"]);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        });
        return services;
    }
}