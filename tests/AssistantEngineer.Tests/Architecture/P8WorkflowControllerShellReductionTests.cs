using AssistantEngineer.Tests.Architecture.Governance;

namespace AssistantEngineer.Tests.Architecture;

public sealed class P8WorkflowControllerShellReductionTests
{
    [Fact]
    public void ReductionArtifactsExist()
    {
        GovernanceSemanticAssertions.AssertDocumentArtifactsExist(
            ReductionMarkdownPath,
            ReductionJsonPath,
            ReductionSchemaPath);
    }

    [Fact]
    public void ReductionJsonParsesAndNoChangeFlagsRemainFalse()
    {
        using var document = GovernanceJsonTestHelper.Parse(ReductionJsonPath);
        var root = document.RootElement;

        GovernanceSemanticAssertions.AssertJsonBooleanFlagsFalse(
            root,
            [
                "runtimeBehaviorChanged",
                "workflowBehaviorChanged",
                "publicApiChanged",
                "dtoShapesChanged",
                "authorizationSemanticsChanged",
                "calculationPhysicsChanged"
            ]);
    }

    [Fact]
    public void ReductionJsonListsExtractedApiAdapterCollaborators()
    {
        using var document = GovernanceJsonTestHelper.Parse(ReductionJsonPath);
        var collaborators = document.RootElement.GetProperty("apiAdapterCollaboratorsExtracted")
            .EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("src/Backend/AssistantEngineer.Api/Services/Calculations/Composition/IEngineeringWorkflowControllerActionService.cs", collaborators);
        Assert.Contains("src/Backend/AssistantEngineer.Api/Services/Calculations/Composition/EngineeringWorkflowControllerActionService.cs", collaborators);
    }

    [Fact]
    public void ReductionJsonContainsNonClaims()
    {
        using var document = GovernanceJsonTestHelper.Parse(ReductionJsonPath);
        var nonClaims = document.RootElement.GetProperty("nonClaims")
            .EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .ToArray();

        GovernanceSemanticAssertions.AssertNonClaimsContainConcepts(
            nonClaims,
            [
                "No workflow behavior change claim",
                "No public API route change claim",
                "No DTO shape change claim",
                "No authorization behavior change claim",
                "No calculation physics change claim"
            ]);
    }

    [Fact]
    public void P8AuditAndDesignReflectP8_03F()
    {
        using var audit = GovernanceJsonTestHelper.Parse(
            Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "engineering-domain-architecture-audit.json"));
        var workflowFinding = audit.RootElement.GetProperty("findings")
            .EnumerateArray()
            .First(item => string.Equals(item.GetProperty("id").GetString(), "P8-00-F08", StringComparison.Ordinal));

        Assert.Contains(
            workflowFinding.GetProperty("resolutionStatus").GetString(),
            new[] { "PartiallyAddressed", "Addressed", "InProgress" });
        Assert.Equal("P8-03F", workflowFinding.GetProperty("resolutionStage").GetString());

        using var design = GovernanceJsonTestHelper.Parse(
            Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "authorization-workflow-decomposition-design.json"));
        var stage = design.RootElement.GetProperty("proposedStages")
            .EnumerateArray()
            .First(item => string.Equals(item.GetProperty("stage").GetString(), "P8-03F", StringComparison.Ordinal));

        Assert.Equal("Implemented", stage.GetProperty("status").GetString());
    }

    [Fact]
    public void EngineeringWorkflowBoundaryAllowlistIsClosedForP8_03F()
    {
        using var allowlist = GovernanceJsonTestHelper.Parse(
            Path.Combine(TestPaths.RepoRoot, "tests", "fixtures", "architecture", "engineeringworkflow-boundary-allowlist.json"));

        Assert.Empty(allowlist.RootElement.EnumerateArray());
    }

    private static string ReductionMarkdownPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "workflow-controller-shell-reduction.md");

    private static string ReductionJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "workflow-controller-shell-reduction.json");

    private static string ReductionSchemaPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "workflow-controller-shell-reduction.schema.json");
}
