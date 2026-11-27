using Microsoft.AspNetCore.HttpOverrides;
using Rehi.Apis.Auth;
using Rehi.Apis.Endpoints;
using Rehi.Apis.Middleware;

namespace Rehi.Apis;

public static class DependencyInjection
{
    public static IServiceCollection AddPresentation(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
                policy.WithOrigins("http://localhost:3000")
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials());
        });
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor;
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
        });
        services.AddAuthRateLimits();
        services.AddEndpoints(typeof(DependencyInjection).Assembly);
        return services;
    }
}