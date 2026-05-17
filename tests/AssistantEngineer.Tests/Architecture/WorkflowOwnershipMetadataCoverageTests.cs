using System.Text.Json;

namespace AssistantEngineer.Tests.Architecture;

public sealed class WorkflowOwnershipMetadataCoverageTests
{
    [Fact]
    public void CoverageJsonExistsAndParses()
    {
        Assert.True(File.Exists(CoverageJsonPath), $"Missing workflow ownership metadata coverage JSON: {CoverageJsonPath}");

        using var document = JsonDocument.Parse(File.ReadAllText(CoverageJsonPath));
        var root = document.RootElement;

        Assert.True(root.TryGetProperty("version", out _));
        Assert.True(root.TryGetProperty("lastReviewedDate", out _));
        Assert.True(root.TryGetProperty("metadataRecords", out var metadataRecords));
        Assert.True(root.TryGetProperty("resolverCoverage", out var resolverCoverage));
        Assert.True(root.TryGetProperty("nonClaims", out _));

        Assert.Equal(JsonValueKind.Array, metadataRecords.ValueKind);
        Assert.Equal(JsonValueKind.Array, resolverCoverage.ValueKind);
        Assert.True(metadataRecords.GetArrayLength() > 0);
        Assert.True(resolverCoverage.GetArrayLength() > 0);
    }

    [Fact]
    public void ResolverCoverageIncludesWorkflowScenarioAndJobResolvers()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(CoverageJsonPath));
        var methods = document.RootElement
            .GetProperty("resolverCoverage")
            .EnumerateArray()
            .Select(entry => entry.GetProperty("resolverMethod").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("ResolveWorkflowScopeAsync", methods);
        Assert.Contains("ResolveScenarioScopeAsync", methods);
        Assert.Contains("ResolveJobScopeAsync", methods);
    }

    [Fact]
    public void MetadataCoverageClaimsRemainEvidenceBasedForWorkflowAndJobPaths()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(CoverageJsonPath));

        var metadataRecords = document.RootElement
            .GetProperty("metadataRecords")
            .EnumerateArray()
            .ToArray();

        var workflowState = Assert.Single(metadataRecords, record =>
            string.Equals(record.GetProperty("recordType").GetString(), "WorkflowState", StringComparison.Ordinal));
        var scenarioRecord = Assert.Single(metadataRecords, record =>
            string.Equals(record.GetProperty("recordType").GetString(), "ScenarioRecord", StringComparison.Ordinal));
        var jobRecord = Assert.Single(metadataRecords, record =>
            string.Equals(record.GetProperty("recordType").GetString(), "JobRecord", StringComparison.Ordinal));

        Assert.Equal("Complete", workflowState.GetProperty("tenantScopeCoverage").GetString());
        Assert.Equal("Complete", scenarioRecord.GetProperty("tenantScopeCoverage").GetString());
        Assert.NotEqual("Complete", jobRecord.GetProperty("tenantScopeCoverage").GetString());

        var workflowResolverCoverage = document.RootElement
            .GetProperty("resolverCoverage")
            .EnumerateArray()
            .Single(entry => string.Equals(entry.GetProperty("resolverMethod").GetString(), "ResolveWorkflowScopeAsync", StringComparison.Ordinal));

        Assert.NotEqual("Complete", workflowResolverCoverage.GetProperty("coverage").GetString());
    }

    private static string CoverageJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "workflow-ownership-metadata-coverage.json");
}
