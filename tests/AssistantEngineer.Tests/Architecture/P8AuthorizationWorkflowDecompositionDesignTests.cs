using AssistantEngineer.Tests.Architecture.Governance;

namespace AssistantEngineer.Tests.Architecture;

public sealed class P8AuthorizationWorkflowDecompositionDesignTests
{
    [Fact]
    public void DecompositionDesignArtifactsExist()
    {
        GovernanceDocumentTestHelper.AssertFilesExist(
            DesignMarkdownPath,
            DesignJsonPath,
            DesignSchemaPath);
    }

    [Fact]
    public void DecompositionDesignJsonParsesAndFlagsRemainFalse()
    {
        using var document = GovernanceJsonTestHelper.Parse(DesignJsonPath);
        var root = document.RootElement;

        Assert.False(root.GetProperty("runtimeBehaviorChanged").GetBoolean());
        Assert.False(root.GetProperty("authorizationSemanticsChanged").GetBoolean());
        Assert.False(root.GetProperty("publicApiChanged").GetBoolean());
        Assert.False(root.GetProperty("calculationPhysicsChanged").GetBoolean());
    }

    [Fact]
    public void DecompositionDesignTargetComponentsContainGateFacadeAndCollaborators()
    {
        using var document = GovernanceJsonTestHelper.Parse(DesignJsonPath);
        var componentIds = document.RootElement.GetProperty("targetComponents")
            .EnumerateArray()
            .Select(item => item.GetProperty("id").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);

        var required = new[]
        {
            "P8-03-TARGET-AUTH-GATE-FACADE",
            "P8-03-TARGET-AUTH-PERMISSION-EVALUATOR",
            "P8-03-TARGET-AUTH-SCOPE-EVALUATOR",
            "P8-03-TARGET-AUTH-DECISION-FACTORY",
            "P8-03-TARGET-AUTH-LOGGER",
            "P8-03-TARGET-AUTH-TENANT-MISMATCH",
            "P8-03-TARGET-WORKFLOW-CONTROLLER-ADAPTER",
            "P8-03-TARGET-WORKFLOW-ORCHESTRATION"
        };

        foreach (var id in required)
            Assert.Contains(id, componentIds);
    }

    [Fact]
    public void DecompositionDesignContainsP8_03AThroughP8_03FStages()
    {
        using var document = GovernanceJsonTestHelper.Parse(DesignJsonPath);
        var stages = document.RootElement.GetProperty("proposedStages")
            .EnumerateArray()
            .Select(item => item.GetProperty("stage").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);

        var required = new[] { "P8-03A", "P8-03B", "P8-03C", "P8-03D", "P8-03E", "P8-03F" };
        foreach (var stage in required)
            Assert.Contains(stage, stages);
    }

    [Fact]
    public void DecompositionDesignCompatibilityRequirementsContainNoChangeConstraints()
    {
        using var document = GovernanceJsonTestHelper.Parse(DesignJsonPath);
        var requirements = document.RootElement.GetProperty("compatibilityRequirements")
            .EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .ToArray();

        Assert.Contains(requirements, value => value.Contains("No public API route change", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(requirements, value => value.Contains("No DTO shape change", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(requirements, value => value.Contains("No authorization semantics change", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(requirements, value => value.Contains("No calculation physics change", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void DecompositionDesignNonClaimsArePresent()
    {
        using var document = GovernanceJsonTestHelper.Parse(DesignJsonPath);
        var nonClaims = document.RootElement.GetProperty("nonClaims")
            .EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .ToArray();

        Assert.Contains(nonClaims, value => value.Contains("No authorization behavior change claim", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(nonClaims, value => value.Contains("No public API route change claim", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(nonClaims, value => value.Contains("No DTO shape change claim", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(nonClaims, value => value.Contains("No calculation physics change claim", StringComparison.OrdinalIgnoreCase));
    }

    private static string DesignMarkdownPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "authorization-workflow-decomposition-design.md");

    private static string DesignJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "authorization-workflow-decomposition-design.json");

    private static string DesignSchemaPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "authorization-workflow-decomposition-design.schema.json");
}
