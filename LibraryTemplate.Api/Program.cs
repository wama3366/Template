using LibraryTemplate.Api.BackgroundJobs;
using LibraryTemplate.Api.Errors;
using LibraryTemplate.Api.Options;
using LibraryTemplate.Api.Security;
using LibraryTemplate.Application;
using LibraryTemplate.Infrastructure;
using LibraryTemplate.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Serilog;
using Serilog.Formatting.Json;

namespace LibraryTemplate.Api;

public class Program
{
    public static void Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console(new JsonFormatter())
            .CreateBootstrapLogger();

        try
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Host.UseSerilog((context, services, loggerConfiguration) =>
            {
                loggerConfiguration
                    .ReadFrom.Configuration(context.Configuration)
                    .ReadFrom.Services(services)
                    .Enrich.FromLogContext()
                    .WriteTo.Console(new JsonFormatter());
            });

            builder.Services.AddOptions<Auth0Options>()
                .BindConfiguration(Auth0Options.SectionName)
                .ValidateDataAnnotations()
                .Validate(options => options.Domain.StartsWith("https://", StringComparison.OrdinalIgnoreCase), "Auth0 domain must be an HTTPS URL.")
                .ValidateOnStart();

            var auth0Options = builder.Configuration
                .GetSection(Auth0Options.SectionName)
                .Get<Auth0Options>() ?? new Auth0Options();

            builder.Services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.Authority = auth0Options.Domain;
                    options.Audience = auth0Options.Audience;
                    options.MapInboundClaims = false;
                });

            builder.Services.AddAuthorization();
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddScoped<ICurrentUserProvider, HttpCurrentUserProvider>();

            builder.Services.AddApplicationServices();
            builder.Services.AddInfrastructureServices(builder.Configuration);
            builder.Services.AddPersistenceServices(builder.Configuration);

            builder.Services.AddOptions<BackgroundJobOptions>()
                .BindConfiguration(BackgroundJobOptions.SectionName)
                .ValidateDataAnnotations()
                .ValidateOnStart();
            builder.Services.AddHostedService<OverdueLoanReminderWorker>();

            builder.Services.AddControllers();
            builder.Services.AddOpenApi();
            builder.Services.AddProblemDetails();
            builder.Services.AddExceptionHandler<ApiExceptionHandler>();
            builder.Services.AddHealthChecks();

            var app = builder.Build();

            app.UseSerilogRequestLogging();
            app.UseExceptionHandler();

            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();
            app.MapHealthChecks("/health");

            app.Run();
        }
        catch (Exception exception)
        {
            Log.Fatal(exception, "API host terminated unexpectedly");
            throw;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
