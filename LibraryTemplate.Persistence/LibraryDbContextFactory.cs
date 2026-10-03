using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace LibraryTemplate.Persistence;

public sealed class LibraryDbContextFactory : IDesignTimeDbContextFactory<LibraryDbContext>
{
    public LibraryDbContext CreateDbContext(string[] args)
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        var appSettingsPath = Path.Combine(currentDirectory, "LibraryTemplate.Api", "appsettings.json");

        if (!File.Exists(appSettingsPath))
        {
            appSettingsPath = Path.GetFullPath(Path.Combine(currentDirectory, "..", "LibraryTemplate.Api", "appsettings.json"));
        }

        var configuration = new ConfigurationBuilder()
            .AddJsonFile(appSettingsPath, optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration["Database:ConnectionString"];

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Database:ConnectionString is required for EF design-time operations.");
        }

        var options = new DbContextOptionsBuilder<LibraryDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new LibraryDbContext(options);
    }
}
