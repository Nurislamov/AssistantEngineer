using AssistantEngineer.Api.Services.Calculations.Persistence;

namespace AssistantEngineer.Tests.Architecture;

public class P3InMemoryPersistenceSyncRootGuardTests
{
    [Fact]
    public void EngineeringWorkflowMemoryStoreDoesNotExposeSyncRootProperty()
    {
        var syncRoot = typeof(EngineeringWorkflowMemoryStore).GetProperty("SyncRoot");
        Assert.Null(syncRoot);
    }

    [Fact]
    public void InMemoryWorkflowPersistenceSourcesDoNotUseGlobalStoreSyncRootLocking()
    {
        var persistenceDirectory = Path.Combine(
            TestPaths.ApiProjectPath,
            "Services",
            "Calculations",
            "Persistence");

        var inMemoryFiles = Directory
            .EnumerateFiles(persistenceDirectory, "InMemoryEngineering*.cs", SearchOption.TopDirectoryOnly)
            .ToArray();

        Assert.NotEmpty(inMemoryFiles);

        foreach (var file in inMemoryFiles)
        {
            var source = File.ReadAllText(file);
            Assert.DoesNotContain("SyncRoot", source, StringComparison.Ordinal);
            Assert.DoesNotContain("lock(_store.SyncRoot)", source, StringComparison.Ordinal);
            Assert.DoesNotContain("lock (_store.SyncRoot)", source, StringComparison.Ordinal);
        }

        var memoryStoreSource = File.ReadAllText(Path.Combine(persistenceDirectory, "EngineeringWorkflowMemoryStore.cs"));
        Assert.DoesNotContain("SyncRoot", memoryStoreSource, StringComparison.Ordinal);
    }
}
