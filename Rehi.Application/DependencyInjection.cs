using System.Reflection;
using System.Runtime.CompilerServices;
using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using FluentValidation;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenAI;
using Rehi.Application.Abstraction.Behaviors;
using Rehi.Application.Articles.CreateArticle;
using Rehi.Application.Chat;

namespace Rehi.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);

            config.AddOpenBehavior(typeof(ExceptionHandlingPipelineBehavior<,>));
            config.AddOpenBehavior(typeof(LoggingPipelineBehavior<,>));
            config.AddOpenBehavior(typeof(ValidationPipelineBehavior<,>));
        });
        services.AddSingleton<ArticleIndexingService>();
        services.AddSingleton<RagChatService>();
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);
        return services;
    }
}