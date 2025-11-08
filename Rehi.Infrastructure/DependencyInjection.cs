using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PayOS;
using Quartz;
using Rehi.Application;
using Rehi.Application.Abstraction.Authentication;
using Rehi.Application.Abstraction.Clock;
using Rehi.Application.Abstraction.Data;
using Rehi.Application.Abstraction.Email;
using Rehi.Application.Abstraction.Payments;
using Rehi.Application.Abstraction.Paypal;
using Rehi.Domain.Common;
using Rehi.Infrastructure.Authentication;
using Rehi.Infrastructure.Clock;
using Rehi.Infrastructure.Database;
using Rehi.Infrastructure.EmailService;
using Rehi.Infrastructure.Outbox;
using Rehi.Infrastructure.Payment;
using Rehi.Infrastructure.Payment.PayOS;
using Rehi.Infrastructure.Payment.Paypal;
using Rehi.Infrastructure.Paypal;
using IConfiguration = Microsoft.Extensions.Configuration.IConfiguration;

namespace Rehi.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        return services
            .AddDomainEventHandlers()
            .AddDatabase(configuration)
            .AddHealthChecks(configuration)
            .AddAuthenticationInternal(configuration)
            .AddConfigServices(configuration)
            .AddPayOsClient();
    }

    private static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddSingleton<InsertOutboxMessagesInterceptor>();
        services.AddDbContext<IDbContext, ApplicationDbContext>((sp, options) =>
            options
                .UseNpgsql(
                    configuration.GetConnectionString("Database"),
                    npgsqlOptions => npgsqlOptions
                        .EnableRetryOnFailure()
                        .MigrationsHistoryTable(HistoryRepository.DefaultTableName, Schemas.Default))
                .AddInterceptors(sp.GetRequiredService<InsertOutboxMessagesInterceptor>()));

        services.Configure<OutboxOptions>(configuration.GetSection("Rehi:Outbox"));
        services.ConfigureOptions<ConfigureProcessOutboxJob>();
        services.TryAddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddScoped<IUserContext, UserContext>();
        services.AddScoped<IPaymentFactory, PaymentFactory>();
        services.AddPayPalHttpClient(configuration);
        services.AddHttpClient<PayPalPaymentService>(); // PayPal cần HttpClient
        services.AddScoped<PayOsPaymentService>();
        services.AddScoped<IPaymentFactory, PaymentFactory>();        
        services.AddScoped<IPayPalWebHookService, PayPalWebhookService>();
        //subscription
        //need to refactor later
        services.AddScoped<ISendEmailService, SendEmailService>();
        services.AddQuartz(configurator =>
        {
            var scheduler = Guid.NewGuid();
            configurator.SchedulerId = $"default-id-{scheduler}";
            configurator.SchedulerName = $"default-name-{scheduler}";

            configurator.AddJob<EmailReminderJob>(c => c
                .StoreDurably()
                .WithIdentity(EmailReminderJob.Name));

            configurator.UsePersistentStore(persistenceOptions =>
            {
                persistenceOptions.UsePostgres(cfg =>
                    {
                        cfg.ConnectionString = configuration.GetConnectionString("Database");
                        cfg.TablePrefix = "public.qrtz_";
                    },
                    "reminders");

                persistenceOptions.UseNewtonsoftJsonSerializer();
                persistenceOptions.UseProperties = true;
            });
        });

        services.AddQuartzHostedService(options => { options.WaitForJobsToComplete = true; });

        return services;
    }

    private static IServiceCollection AddHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddHealthChecks()
            .AddNpgSql(configuration.GetConnectionString("Database")!);
        return services;
    }

    private static IServiceCollection AddDomainEventHandlers(this IServiceCollection services)
    {
        var domainEventHandlers = AssemblyReference.Assembly
            .GetTypes()
            .Where(t => t.IsAssignableTo(typeof(IDomainEventHandler)))
            .ToArray();

        foreach (var domainEventHandler in domainEventHandlers)
        {
            services.TryAddScoped(domainEventHandler);

            var domainEvent = domainEventHandler
                .GetInterfaces()
                .Single(i => i.IsGenericType)
                .GetGenericArguments()
                .Single();

            var closedIdempotentHandler = typeof(IdempotentDomainEventHandler<>).MakeGenericType(domainEvent);

            services.Decorate(domainEventHandler, closedIdempotentHandler);
        }

        return services;
    }

    private static IServiceCollection AddAuthenticationInternal(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var auth0Options = configuration.GetSection("Auth0").Get<Auth0Options>()!;

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = $"https://{auth0Options.Domain}";
                options.Audience = auth0Options.Audience;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateAudience = true,
                    ValidateIssuer = true
                };
            });
        services.AddHttpContextAccessor();
        return services;
    }

    private static IServiceCollection AddConfigServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<PayOsOptions>()
            .Bind(configuration.GetSection(PayOsOptions.PayOs))
            .ValidateDataAnnotations();
        
        services.AddOptions<SubscriptionOptions>()
            .Bind(configuration.GetSection(SubscriptionOptions.Subscription))
            .ValidateDataAnnotations();
        
        return services;
    }

    private static IServiceCollection AddPayOsClient(this IServiceCollection services)
    {
        services.AddKeyedSingleton("PayOsClient", (sp, key) =>
        {
            var logger = sp.GetRequiredService<ILogger<PayOSClient>>();
            try
            {
                var options = sp.GetRequiredService<IOptions<PayOsOptions>>().Value;

                if (string.IsNullOrWhiteSpace(options.ClientId) ||
                    string.IsNullOrWhiteSpace(options.ApiKey) ||
                    string.IsNullOrWhiteSpace(options.CheckSum))
                {
                    throw new InvalidOperationException("PayOS configuration is missing required values (ClientId / ApiKey / ChecksumKey).");
                }

                return new PayOSClient(new PayOSOptions
                {
                    ClientId = options.ClientId,
                    ApiKey = options.ApiKey,
                    ChecksumKey = options.CheckSum,
                    LogLevel = LogLevel.Debug,
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to initialize PayOS client for key: {ClientKey}", key);
                throw; 
            }
        });

        return services;
    }

}