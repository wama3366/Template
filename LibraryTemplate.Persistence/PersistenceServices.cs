using LibraryTemplate.Application.Dependencies.Persistence;
using LibraryTemplate.Domain.Dependencies.Persistence;
using LibraryTemplate.Persistence.Options;
using LibraryTemplate.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LibraryTemplate.Persistence;

public static class PersistenceServices
{
    public static IServiceCollection AddPersistenceServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<DatabaseOptions>()
            .BindConfiguration(DatabaseOptions.SectionName)
            .ValidateDataAnnotations()
            .Validate(options => !string.IsNullOrWhiteSpace(options.ConnectionString), "A database connection string is required.")
            .ValidateOnStart();

        services.AddDbContext<LibraryDbContext>((serviceProvider, options) =>
        {
            var database = serviceProvider.GetRequiredService<IOptions<DatabaseOptions>>().Value;
            options.UseNpgsql(database.ConnectionString);
        });

        services.AddScoped<IBranchRepository, BranchRepository>();
        services.AddScoped<IAuthorRepository, AuthorRepository>();
        services.AddScoped<IBookRepository, BookRepository>();
        services.AddScoped<ILoanRepository, LoanRepository>();
        services.AddScoped<ILoanOverviewRepository, LoanOverviewRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserBranchRepository, UserBranchRepository>();
        services.AddScoped<IBorrowerProfileRepository, BorrowerProfileRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
