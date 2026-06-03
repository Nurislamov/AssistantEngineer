using AssistantEngineer.Modules.EngineeringWorkflow;
using AssistantEngineer.Modules.EngineeringWorkflow.Application.Abstractions.Artifacts;
using AssistantEngineer.Modules.EngineeringWorkflow.Application.Contracts.Artifacts;
using AssistantEngineer.Modules.EngineeringWorkflow.Application.Services.Artifacts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Tests.EngineeringWorkflow.Artifacts;

public sealed class EngineeringArtifactStorageDependencyInjectionTests
{
    [Fact]
    public void AddEngineeringWorkflowModule_ResolvesInMemoryProviderByDefault()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        services.AddEngineeringWorkflowModule(configuration);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var storage = scope.ServiceProvider.GetRequiredService<IEngineeringArtifactStorage>();
        Assert.IsType<InMemoryEngineeringArtifactStorage>(storage);

        var options = scope.ServiceProvider.GetRequiredService<IOptions<EngineeringArtifactStorageOptions>>().Value;
        Assert.Equal(EngineeringArtifactStorageProviders.InMemory, options.Provider);
        Assert.True(options.MaxArtifactBytes > 0);
    }

    [Fact]
    public void AddEngineeringWorkflowModule_ResolvesFileSystemProviderWhenConfigured()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"assistant-engineer-artifacts-di-{Guid.NewGuid():N}");
        try
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["EngineeringArtifacts:Provider"] = "FileSystem",
                    ["EngineeringArtifacts:RootPath"] = tempRoot,
                    ["EngineeringArtifacts:MaxArtifactBytes"] = "2048",
                    ["EngineeringArtifacts:EnableSha256Verification"] = "true"
                })
                .Build();

            var services = new ServiceCollection();
            services.AddEngineeringWorkflowModule(configuration);

            using var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();

            var storage = scope.ServiceProvider.GetRequiredService<IEngineeringArtifactStorage>();
            Assert.IsType<FileSystemEngineeringArtifactStorage>(storage);

            Assert.True(Directory.Exists(tempRoot));
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    [Fact]
    public void AddEngineeringWorkflowModule_RejectsInvalidFileSystemConfiguration()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["EngineeringArtifacts:Provider"] = "FileSystem",
                ["EngineeringArtifacts:RootPath"] = "",
                ["EngineeringArtifacts:MaxArtifactBytes"] = "0"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddEngineeringWorkflowModule(configuration);

        using var provider = services.BuildServiceProvider();
        var exception = Assert.Throws<OptionsValidationException>(() =>
            provider.GetRequiredService<IOptions<EngineeringArtifactStorageOptions>>().Value);

        Assert.Contains(
            exception.Failures,
            failure => failure.Contains("EngineeringArtifacts:MaxArtifactBytes", StringComparison.Ordinal));
    }
}
