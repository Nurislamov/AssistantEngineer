using AssistantEngineer.Api.Services.Calculations.Persistence.Durable;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Api.Services.Calculations.Persistence;

internal static class EngineeringWorkflowPersistenceRegistration
{
    public static IServiceCollection AddEngineeringWorkflowPersistence(this IServiceCollection services)
    {
        services.AddOptions<EngineeringWorkflowPersistenceOptions>()
            .Configure<IConfiguration>((options, configuration) =>
            {
                configuration.GetSection(EngineeringWorkflowPersistenceOptions.SectionName).Bind(options);

                if (string.IsNullOrWhiteSpace(options.SqliteConnectionString))
                {
                    options.SqliteConnectionString = configuration.GetConnectionString("EngineeringWorkflowPersistence");
                }
            });

        services.AddSingleton<EngineeringWorkflowMemoryStore>();
        services.AddSingleton<EngineeringWorkflowPersistenceDatabaseInitializer>();

        services.AddDbContext<EngineeringWorkflowPersistenceDbContext>((serviceProvider, optionsBuilder) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<EngineeringWorkflowPersistenceOptions>>().Value;
            var sqliteConnectionString = ResolveSqliteConnectionString(options);

            optionsBuilder.UseSqlite(sqliteConnectionString);
        });

        services.AddScoped<IEngineeringProjectRepository>(serviceProvider =>
        {
            var provider = ResolveProvider(serviceProvider);
            if (provider == EngineeringWorkflowPersistenceProvider.SQLite)
            {
                return new EfEngineeringProjectRepository(serviceProvider.GetRequiredService<EngineeringWorkflowPersistenceDbContext>());
            }

            return new InMemoryEngineeringProjectRepository(serviceProvider.GetRequiredService<EngineeringWorkflowMemoryStore>());
        });

        services.AddScoped<IEngineeringWorkflowStateRepository>(serviceProvider =>
        {
            var provider = ResolveProvider(serviceProvider);
            if (provider == EngineeringWorkflowPersistenceProvider.SQLite)
            {
                return new EfEngineeringWorkflowStateRepository(serviceProvider.GetRequiredService<EngineeringWorkflowPersistenceDbContext>());
            }

            return new InMemoryEngineeringWorkflowStateRepository(serviceProvider.GetRequiredService<EngineeringWorkflowMemoryStore>());
        });

        services.AddScoped<IEngineeringCalculationScenarioRepository>(serviceProvider =>
        {
            var provider = ResolveProvider(serviceProvider);
            if (provider == EngineeringWorkflowPersistenceProvider.SQLite)
            {
                return new EfEngineeringCalculationScenarioRepository(serviceProvider.GetRequiredService<EngineeringWorkflowPersistenceDbContext>());
            }

            return new InMemoryEngineeringCalculationScenarioRepository(serviceProvider.GetRequiredService<EngineeringWorkflowMemoryStore>());
        });

        services.AddScoped<IEngineeringCalculationArtifactRepository>(serviceProvider =>
        {
            var provider = ResolveProvider(serviceProvider);
            if (provider == EngineeringWorkflowPersistenceProvider.SQLite)
            {
                return new EfEngineeringCalculationArtifactRepository(serviceProvider.GetRequiredService<EngineeringWorkflowPersistenceDbContext>());
            }

            return new InMemoryEngineeringCalculationArtifactRepository(serviceProvider.GetRequiredService<EngineeringWorkflowMemoryStore>());
        });

        services.AddScoped<IEngineeringScenarioHistoryRepository>(serviceProvider =>
        {
            var provider = ResolveProvider(serviceProvider);
            if (provider == EngineeringWorkflowPersistenceProvider.SQLite)
            {
                return new EfEngineeringScenarioHistoryRepository(serviceProvider.GetRequiredService<EngineeringWorkflowPersistenceDbContext>());
            }

            return new InMemoryEngineeringScenarioHistoryRepository(serviceProvider.GetRequiredService<EngineeringWorkflowMemoryStore>());
        });

        services.AddScoped<IEngineeringWorkflowPersistenceService>(serviceProvider =>
        {
            var provider = ResolveProvider(serviceProvider);
            if (provider == EngineeringWorkflowPersistenceProvider.SQLite)
            {
                var initializer = serviceProvider.GetRequiredService<EngineeringWorkflowPersistenceDatabaseInitializer>();
                var context = serviceProvider.GetRequiredService<EngineeringWorkflowPersistenceDbContext>();
                initializer.EnsureInitialized(context);
            }

            return new EngineeringWorkflowPersistenceService(
                serviceProvider.GetRequiredService<IEngineeringProjectRepository>(),
                serviceProvider.GetRequiredService<IEngineeringWorkflowStateRepository>(),
                serviceProvider.GetRequiredService<IEngineeringCalculationScenarioRepository>(),
                serviceProvider.GetRequiredService<IEngineeringCalculationArtifactRepository>(),
                serviceProvider.GetRequiredService<IEngineeringScenarioHistoryRepository>(),
                serviceProvider.GetRequiredService<IOptions<EngineeringWorkflowPersistenceOptions>>());
        });

        return services;
    }

    private static EngineeringWorkflowPersistenceProvider ResolveProvider(IServiceProvider serviceProvider)
    {
        var options = serviceProvider.GetRequiredService<IOptions<EngineeringWorkflowPersistenceOptions>>().Value;
        return options.Provider switch
        {
            EngineeringWorkflowPersistenceProvider.SQLite => EngineeringWorkflowPersistenceProvider.SQLite,
            EngineeringWorkflowPersistenceProvider.None => EngineeringWorkflowPersistenceProvider.InMemory,
            _ => EngineeringWorkflowPersistenceProvider.InMemory
        };
    }

    private static string ResolveSqliteConnectionString(EngineeringWorkflowPersistenceOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.SqliteConnectionString))
        {
            return options.SqliteConnectionString;
        }

        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = "assistant-engineer-workflow.db",
            Cache = SqliteCacheMode.Shared,
            Mode = SqliteOpenMode.ReadWriteCreate
        };

        return builder.ToString();
    }
}
