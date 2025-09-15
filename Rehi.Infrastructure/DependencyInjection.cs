using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Quartz;
using Rehi.Application.Abstraction.Clock;
using Rehi.Application.Abstraction.Data;
using Rehi.Domain.Common;
using Rehi.Infrastructure.Clock;
using Rehi.Infrastructure.Database;
using Rehi.Infrastructure.Outbox;

namespace Rehi.Infrastructure;

public static class DependencyInjection
{
    
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration) =>
        services
            .AddDomainEventHandlers()
            .AddDatabase(configuration)
            .AddHealthChecks(configuration);
            // .AddAuthenticationInternal(configuration);


            private static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
            {
                services.TryAddSingleton<InsertOutboxMessagesInterceptor>();
                services.AddDbContext<IDbContext, ApplicationDbContext>((sp, options) =>
                    options
                        .UseNpgsql(
                            configuration.GetConnectionString("Database"),
                            npgsqlOptions => npgsqlOptions
                                .MigrationsHistoryTable(HistoryRepository.DefaultTableName, Schemas.Default))
                        .AddInterceptors(sp.GetRequiredService<InsertOutboxMessagesInterceptor>()));
                
                services.Configure<OutboxOptions>(configuration.GetSection("Rehi:Outbox"));
                services.ConfigureOptions<ConfigureProcessOutboxJob>();
                services.TryAddSingleton<IDateTimeProvider, DateTimeProvider>();
                services.AddQuartz(configurator =>
                {
                    var scheduler = Guid.NewGuid();
                    configurator.SchedulerId = $"default-id-{scheduler}";
                    configurator.SchedulerName = $"default-name-{scheduler}";
                });

                services.AddQuartzHostedService(options =>
                {
                    options.WaitForJobsToComplete = true;
                });
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
        Type[] domainEventHandlers = Application.AssemblyReference.Assembly
            .GetTypes()
            .Where(t => t.IsAssignableTo(typeof(IDomainEventHandler)))
            .ToArray();

        foreach (Type domainEventHandler in domainEventHandlers)
        {
            services.TryAddScoped(domainEventHandler);

            Type domainEvent = domainEventHandler
                .GetInterfaces()
                .Single(i => i.IsGenericType)
                .GetGenericArguments()
                .Single();

            Type closedIdempotentHandler = typeof(IdempotentDomainEventHandler<>).MakeGenericType(domainEvent);

            services.Decorate(domainEventHandler, closedIdempotentHandler);
        }
        return services;
    }
    // private static IServiceCollection AddAuthenticationInternal(
    //     this IServiceCollection services,
    //     IConfiguration configuration)
    // {
    //     services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    //         .AddJwtBearer(o =>
    //         {
    //             o.RequireHttpsMetadata = false;
    //             o.TokenValidationParameters = new TokenValidationParameters()
    //             {
    //                 IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Secret"]!)),
    //                 ValidIssuer = configuration["Jwt:Issuer"],
    //                 ValidAudience = configuration["Jwt:Audience"],
    //                 ClockSkew = TimeSpan.Zero
    //             };
    //         });
    //
    //     services.AddHttpContextAccessor();
    //     return services;
    // }
}



    
