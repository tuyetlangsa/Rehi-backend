using System.Threading.RateLimiting;

namespace Rehi.Apis.Auth;

public static class AuthRateLimits
{
    public const string Login = "auth-login";
    public const string Register = "auth-register";
    public const string ResendVerification = "auth-resend-verification";

    private static readonly (string Policy, int PermitsPerMinute)[] Policies =
    [
        (Login, 10),
        (Register, 5),
        (ResendVerification, 5)
    ];

    public static IServiceCollection AddAuthRateLimits(this IServiceCollection services) =>
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            // RemoteIpAddress is the TCP peer. X-Forwarded-For rewrites it only when the deployment sets
            // ASPNETCORE_FORWARDEDHEADERS_ENABLED=true, which an app behind Azure App Service must do.
            foreach (var (policy, permitsPerMinute) in Policies)
                options.AddPolicy(policy, context => RateLimitPartition.GetFixedWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = permitsPerMinute,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    }));
        });
}
