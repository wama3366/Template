using LibraryTemplate.Application.Dependencies.ExternalServices;
using LibraryTemplate.Application.Dependencies.Infrastructure;
using LibraryTemplate.Infrastructure.Email;
using LibraryTemplate.Infrastructure.Options;
using LibraryTemplate.Infrastructure.Time;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LibraryTemplate.Infrastructure;

public static class InfrastructureServices
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<EmailOptions>()
            .BindConfiguration(EmailOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IEmailSender, LoggingEmailSender>();

        return services;
    }
}
