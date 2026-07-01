using AssistantEngineer.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Infrastructure.DatabaseMigrations;

public static class PostgresMigrationRunnerRegistration
{
    public static IServiceCollection AddPostgresMigrationRunner(
        this IServiceCollection services,
        IConfiguration configuration,
        string environmentName,
        TextWriter output,
        TextWriter error)
    {
        services.AddAppDbContextPersistence(configuration, environmentName);
        services.AddScoped<IPostgresMigrationDatabase, AppDbContextPostgresMigrationDatabase>();
        services.AddScoped<PostgresMigrationRunner>();
        services.AddSingleton<IPostgresMigrationReporter>(
            new ConsolePostgresMigrationReporter(output, error));

        return services;
    }
}
