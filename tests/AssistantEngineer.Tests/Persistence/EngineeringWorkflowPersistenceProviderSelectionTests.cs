using AssistantEngineer.Api.Configuration;
using AssistantEngineer.Api.Services.Calculations.Persistence;
using AssistantEngineer.Api.Services.Calculations.Persistence.Durable;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Tests.Persistence;

public class EngineeringWorkflowPersistenceProviderSelectionTests
{
    [Fact]
    public void InMemoryProviderResolvesInMemoryRepositoriesAndNonDurableMetadata()
    {
        using var provider = BuildServiceProvider(new Dictionary<string, string?>
        {
            [$"{EngineeringWorkflowPersistenceOptions.SectionName}:Provider"] = "InMemory"
        });

        using var scope = provider.CreateScope();
        var services = scope.ServiceProvider;

        Assert.IsType<InMemoryEngineeringProjectRepository>(services.GetRequiredService<IEngineeringProjectRepository>());
        Assert.IsType<InMemoryEngineeringWorkflowStateRepository>(services.GetRequiredService<IEngineeringWorkflowStateRepository>());
        Assert.IsType<InMemoryEngineeringCalculationScenarioRepository>(services.GetRequiredService<IEngineeringCalculationScenarioRepository>());
        Assert.IsType<InMemoryEngineeringCalculationArtifactRepository>(services.GetRequiredService<IEngineeringCalculationArtifactRepository>());
        Assert.IsType<InMemoryEngineeringScenarioHistoryRepository>(services.GetRequiredService<IEngineeringScenarioHistoryRepository>());
        Assert.IsType<InMemoryEngineeringCalculationJobRepository>(services.GetRequiredService<IEngineeringCalculationJobRepository>());
        Assert.IsType<InMemoryEngineeringCalculationJobEventRepository>(services.GetRequiredService<IEngineeringCalculationJobEventRepository>());

        var persistenceService = services.GetRequiredService<IEngineeringWorkflowPersistenceService>();
        var info = persistenceService.GetProviderInfo();
        Assert.Equal(EngineeringWorkflowPersistenceProvider.InMemory, info.Provider);
        Assert.False(info.DurableEnabled);
    }

    [Fact]
    public void SqliteProviderResolvesDurableRepositoriesAndDurableMetadata()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"assistant-engineer-stage12-{Guid.NewGuid():N}.db");
        try
        {
            using var provider = BuildServiceProvider(new Dictionary<string, string?>
            {
                [$"{EngineeringWorkflowPersistenceOptions.SectionName}:Provider"] = "SQLite",
                [$"{EngineeringWorkflowPersistenceOptions.SectionName}:EnsureCreatedOnStartup"] = "true",
                [$"{EngineeringWorkflowPersistenceOptions.SectionName}:SqliteConnectionString"] = $"Data Source={dbPath};Cache=Shared;Mode=ReadWriteCreate"
            });

            using var scope = provider.CreateScope();
            var services = scope.ServiceProvider;

            Assert.IsType<EfEngineeringProjectRepository>(services.GetRequiredService<IEngineeringProjectRepository>());
            Assert.IsType<EfEngineeringWorkflowStateRepository>(services.GetRequiredService<IEngineeringWorkflowStateRepository>());
            Assert.IsType<EfEngineeringCalculationScenarioRepository>(services.GetRequiredService<IEngineeringCalculationScenarioRepository>());
            Assert.IsType<EfEngineeringCalculationArtifactRepository>(services.GetRequiredService<IEngineeringCalculationArtifactRepository>());
            Assert.IsType<EfEngineeringScenarioHistoryRepository>(services.GetRequiredService<IEngineeringScenarioHistoryRepository>());
            Assert.IsType<EfEngineeringCalculationJobRepository>(services.GetRequiredService<IEngineeringCalculationJobRepository>());
            Assert.IsType<EfEngineeringCalculationJobEventRepository>(services.GetRequiredService<IEngineeringCalculationJobEventRepository>());

            var persistenceService = services.GetRequiredService<IEngineeringWorkflowPersistenceService>();
            var info = persistenceService.GetProviderInfo();
            Assert.Equal(EngineeringWorkflowPersistenceProvider.SQLite, info.Provider);
            Assert.True(info.DurableEnabled);
            Assert.True(File.Exists(dbPath));
        }
        finally
        {
            if (File.Exists(dbPath))
            {
                TryDeleteFile(dbPath);
            }
        }
    }

    [Fact]
    public void SqliteProviderCanUseConnectionStringFallbackKey()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"assistant-engineer-stage12-fallback-{Guid.NewGuid():N}.db");
        try
        {
            using var provider = BuildServiceProvider(new Dictionary<string, string?>
            {
                [$"{EngineeringWorkflowPersistenceOptions.SectionName}:Provider"] = "SQLite",
                ["ConnectionStrings:EngineeringWorkflowPersistence"] = $"Data Source={dbPath};Cache=Shared;Mode=ReadWriteCreate"
            });

            using var scope = provider.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IEngineeringWorkflowPersistenceService>();
            var info = service.GetProviderInfo();

            Assert.Equal(EngineeringWorkflowPersistenceProvider.SQLite, info.Provider);
            Assert.True(File.Exists(dbPath));
        }
        finally
        {
            if (File.Exists(dbPath))
            {
                TryDeleteFile(dbPath);
            }
        }
    }

    private static ServiceProvider BuildServiceProvider(IReadOnlyDictionary<string, string?> values)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();
        services.AddApiPresentation();
        return services.BuildServiceProvider();
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
            // SQLite file cleanup is best-effort in tests.
        }
    }
}
