using AssistantEngineer.Tests.Architecture.Governance;

namespace AssistantEngineer.Tests.Architecture;

public sealed class P8WorkflowControllerShellCharacterizationTests
{
    [Fact]
    public void CharacterizationArtifactsExist()
    {
        GovernanceDocumentTestHelper.AssertFilesExist(
            CharacterizationMarkdownPath,
            CharacterizationJsonPath,
            CharacterizationSchemaPath);
    }

    [Fact]
    public void CharacterizationJsonParsesAndFlagsRemainFalse()
    {
        using var document = GovernanceJsonTestHelper.Parse(CharacterizationJsonPath);
        var root = document.RootElement;

        Assert.False(root.GetProperty("runtimeBehaviorChanged").GetBoolean());
        Assert.False(root.GetProperty("workflowBehaviorChanged").GetBoolean());
        Assert.False(root.GetProperty("publicApiChanged").GetBoolean());
        Assert.False(root.GetProperty("dtoShapesChanged").GetBoolean());
        Assert.False(root.GetProperty("calculationPhysicsChanged").GetBoolean());
    }

    [Fact]
    public void CharacterizationJsonIncludesExecutionReadReportArtifactGroups()
    {
        using var document = GovernanceJsonTestHelper.Parse(CharacterizationJsonPath);
        var groups = document.RootElement.GetProperty("characterizedEndpointGroups")
            .EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("Execution", groups);
        Assert.Contains("ReadHistory", groups);
        Assert.Contains("ReportArtifact", groups);
    }

    [Fact]
    public void CharacterizationJsonContainsRepresentativeRoutes()
    {
        using var document = GovernanceJsonTestHelper.Parse(CharacterizationJsonPath);
        var routes = document.RootElement.GetProperty("characterizedRoutes")
            .EnumerateArray()
            .Select(item => item.GetProperty("routeTemplate").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("api/v{version:apiVersion}/engineering-workflow/prepare-calculation", routes);
        Assert.Contains("api/v{version:apiVersion}/engineering-workflow/{projectId:int}/state", routes);
        Assert.Contains("api/v{version:apiVersion}/engineering-workflow/report", routes);
        Assert.Contains("api/v{version:apiVersion}/engineering-workflow/scenarios/{scenarioId}/artifacts/{artifactKind}", routes);
    }

    [Fact]
    public void CharacterizationNonClaimsArePresent()
    {
        using var document = GovernanceJsonTestHelper.Parse(CharacterizationJsonPath);
        var nonClaims = document.RootElement.GetProperty("nonClaims")
            .EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .ToArray();

        Assert.Contains(nonClaims, value => value.Contains("No workflow behavior change claim", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(nonClaims, value => value.Contains("No public API route change claim", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(nonClaims, value => value.Contains("No DTO shape change claim", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(nonClaims, value => value.Contains("No calculation physics change claim", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void P8AuditMarksWorkflowControllerHotspotAsCharacterizedInP8_03D()
    {
        using var audit = GovernanceJsonTestHelper.Parse(
            Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "engineering-domain-architecture-audit.json"));

        var workflowFinding = audit.RootElement.GetProperty("findings")
            .EnumerateArray()
            .First(item => string.Equals(item.GetProperty("id").GetString(), "P8-00-F08", StringComparison.Ordinal));

        Assert.Contains(
            workflowFinding.GetProperty("resolutionStatus").GetString(),
            new[] { "Characterized", "InProgress", "PartiallyAddressed" });
        Assert.Contains(
            workflowFinding.GetProperty("resolutionStage").GetString(),
            new[] { "P8-03D", "P8-03E", "P8-03F" });
    }

    private static string CharacterizationMarkdownPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "workflow-controller-shell-characterization.md");

    private static string CharacterizationJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "workflow-controller-shell-characterization.json");

    private static string CharacterizationSchemaPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "workflow-controller-shell-characterization.schema.json");
}
