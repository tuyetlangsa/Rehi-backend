using Rehi.Apis.Endpoints;
using Rehi.Apis.Extensions;
using Rehi.Apis.Middleware;
using Rehi.Application;
using Rehi.Application.Abstraction.CurrentUser;
using Rehi.Infrastructure;
using Rehi.Infrastructure.Authentication;
using Rehi.Infrastructure.CurrentUser;
using Serilog;

namespace Rehi.Apis;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        
        builder.Host.UseSerilog((context, loggerConfig) => loggerConfig.ReadFrom.Configuration(context.Configuration));
        builder.Services.AddSwaggerGenWithAuth();

        builder.Services
            .AddApplication() // add application handler
            .AddPresentation()
            .AddInfrastructure(builder.Configuration);


        WebApplication app = builder.Build();

        app.UseSwaggerWithUi();
        app.ApplyMigrations();
        app.UseCors();
        app.UseLogContext();
        app.UseSerilogRequestLogging();
        app.UseExceptionHandler();
        app.UseAuthentication();
        // app.UseAuthorization();
        app.MapEndpoints();
        app.Run();
    }
}