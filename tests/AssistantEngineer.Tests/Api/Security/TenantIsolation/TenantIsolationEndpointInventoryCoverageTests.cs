using System.Text.Json;

namespace AssistantEngineer.Tests.Api.Security.TenantIsolation;

public sealed class TenantIsolationEndpointInventoryCoverageTests
{
    [Fact]
    public void MatrixEndpointGroupsMapToEndpointInventoryEntriesOrKnownLimitations()
    {
        using var matrixDocument = JsonDocument.Parse(File.ReadAllText(MatrixJsonPath));
        using var inventoryDocument = JsonDocument.Parse(File.ReadAllText(EndpointInventoryJsonPath));

        var inventoryEndpoints = inventoryDocument.RootElement.GetProperty("endpoints").EnumerateArray().ToArray();
        foreach (var group in matrixDocument.RootElement.GetProperty("endpointGroups").EnumerateArray())
        {
            var groupName = group.GetProperty("group").GetString() ?? string.Empty;
            var permission = group.GetProperty("permission").GetString() ?? string.Empty;
            var rolloutStage = group.GetProperty("rolloutStage").GetString() ?? string.Empty;
            var knownLimitations = group.GetProperty("knownLimitations").EnumerateArray()
                .Select(item => item.GetString() ?? string.Empty)
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .ToArray();

            var hasInventoryEntry = inventoryEndpoints.Any(endpoint =>
                string.Equals(endpoint.GetProperty("rolloutStage").GetString(), rolloutStage, StringComparison.Ordinal) &&
                (endpoint.GetProperty("targetPolicy").GetString() ?? string.Empty).Contains(permission, StringComparison.Ordinal));

            Assert.True(
                hasInventoryEntry || knownLimitations.Length > 0,
                $"Matrix group {groupName} ({permission}, {rolloutStage}) must map to endpoint inventory or document a limitation.");
        }
    }

    [Fact]
    public void P5_10ThroughP5_14RolloutStagesRemainRepresented()
    {
        using var inventoryDocument = JsonDocument.Parse(File.ReadAllText(EndpointInventoryJsonPath));
        var rolloutStages = inventoryDocument.RootElement.GetProperty("endpoints")
            .EnumerateArray()
            .Select(endpoint => endpoint.GetProperty("rolloutStage").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var stage in new[] { "P5-10", "P5-11", "P5-12", "P5-13", "P5-14" })
        {
            Assert.Contains(stage, rolloutStages);
        }
    }

    [Fact]
    public void MatrixCoveredByTestsReferenceExistingTestClasses()
    {
        using var matrixDocument = JsonDocument.Parse(File.ReadAllText(MatrixJsonPath));
        var testFiles = Directory.EnumerateFiles(TestsRoot, "*.cs", SearchOption.AllDirectories)
            .Select(Path.GetFileNameWithoutExtension)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var group in matrixDocument.RootElement.GetProperty("endpointGroups").EnumerateArray())
        {
            var groupName = group.GetProperty("group").GetString() ?? string.Empty;
            var coveredByTests = group.GetProperty("coveredByTests").EnumerateArray()
                .Select(item => item.GetString() ?? string.Empty)
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .ToArray();

            Assert.NotEmpty(coveredByTests);
            foreach (var testName in coveredByTests)
            {
                Assert.Contains(testName, testFiles);
            }

            Assert.DoesNotContain("Placeholder", coveredByTests, StringComparer.OrdinalIgnoreCase);
            Assert.False(coveredByTests.Any(testName => testName.Contains("TODO", StringComparison.OrdinalIgnoreCase)), $"Matrix group {groupName} references TODO test coverage.");
        }
    }

    [Fact]
    public void PartialOrStagedGroupsDocumentKnownLimitations()
    {
        using var matrixDocument = JsonDocument.Parse(File.ReadAllText(MatrixJsonPath));
        var stagedGroups = new HashSet<string>(StringComparer.Ordinal)
        {
            "WorkflowsRead",
            "WorkflowsExecute",
            "CalculationRun",
            "ArtifactRead",
            "WorkflowScenarioRead",
            "WorkflowJobRead",
            "WorkflowJobEventsRead"
        };

        foreach (var group in matrixDocument.RootElement.GetProperty("endpointGroups").EnumerateArray())
        {
            var groupName = group.GetProperty("group").GetString() ?? string.Empty;
            if (!stagedGroups.Contains(groupName))
            {
                continue;
            }

            var limitations = group.GetProperty("knownLimitations").EnumerateArray()
                .Select(item => item.GetString() ?? string.Empty)
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .ToArray();

            Assert.NotEmpty(limitations);
        }
    }

    private static string MatrixJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "tenant-isolation-integration-matrix.json");

    private static string EndpointInventoryJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "api-endpoint-protection-inventory.json");

    private static string TestsRoot =>
        Path.Combine(TestPaths.RepoRoot, "tests", "AssistantEngineer.Tests");
}
