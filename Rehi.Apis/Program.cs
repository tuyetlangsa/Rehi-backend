using Rehi.Apis.Endpoints;
using Rehi.Apis.Extensions;
using Rehi.Apis.Middleware;
using Rehi.Application;
using Rehi.Infrastructure;
using Rehi.Infrastructure.Database;
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
        builder.Services.AddAuthorization();

        WebApplication app = builder.Build();
        
        // // === Seed Database ===
        // using (var scope = app.Services.CreateScope())
        // {
        //     var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        //     DbInitializer.Seed(dbContext);
        // }
        
        app.UseSwaggerWithUi();
        app.ApplyMigrations();
        app.UseCors();
        app.UseLogContext();
        app.UseSerilogRequestLogging();
        app.UseExceptionHandler();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapEndpoints();
        app.Run();
    }
}