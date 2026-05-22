using AssistantEngineer.Tests.Architecture.Governance;

namespace AssistantEngineer.Tests.Architecture;

public sealed class P8ProtectedEndpointAuthorizationGateDecompositionTests
{
    [Fact]
    public void DecompositionArtifactsExist()
    {
        GovernanceDocumentTestHelper.AssertFilesExist(
            DecompositionMarkdownPath,
            DecompositionJsonPath,
            DecompositionSchemaPath);
    }

    [Fact]
    public void DecompositionJsonParsesAndNoChangeFlagsRemainFalse()
    {
        using var document = GovernanceJsonTestHelper.Parse(DecompositionJsonPath);
        var root = document.RootElement;

        Assert.False(root.GetProperty("runtimeBehaviorChanged").GetBoolean());
        Assert.False(root.GetProperty("authorizationSemanticsChanged").GetBoolean());
        Assert.False(root.GetProperty("publicApiChanged").GetBoolean());
        Assert.False(root.GetProperty("calculationPhysicsChanged").GetBoolean());
    }

    [Fact]
    public void DecompositionIncludesPermissionAndScopeCollaborators()
    {
        using var document = GovernanceJsonTestHelper.Parse(DecompositionJsonPath);
        var components = document.RootElement.GetProperty("componentsExtracted")
            .EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("IProtectedEndpointPermissionEvaluator", components);
        Assert.Contains("IProtectedEndpointScopeEvaluationService", components);
    }

    [Fact]
    public void DecompositionNonClaimsArePresent()
    {
        using var document = GovernanceJsonTestHelper.Parse(DecompositionJsonPath);
        var nonClaims = document.RootElement.GetProperty("nonClaims")
            .EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .ToArray();

        Assert.Contains(nonClaims, value => value.Contains("No authorization behavior change claim", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(nonClaims, value => value.Contains("No public API route change claim", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(nonClaims, value => value.Contains("No DTO shape change claim", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(nonClaims, value => value.Contains("No calculation physics change claim", StringComparison.OrdinalIgnoreCase));
    }

    private static string DecompositionMarkdownPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "protected-endpoint-authorization-gate-decomposition.md");

    private static string DecompositionJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "protected-endpoint-authorization-gate-decomposition.json");

    private static string DecompositionSchemaPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "protected-endpoint-authorization-gate-decomposition.schema.json");
}
