using AssistantEngineer.Tests.Architecture.Governance;

namespace AssistantEngineer.Tests.Architecture;

public sealed class P8ProtectedEndpointAuthorizationGateCharacterizationTests
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
    public void CharacterizationJsonParsesAndNoChangeFlagsRemainFalse()
    {
        using var document = GovernanceJsonTestHelper.Parse(CharacterizationJsonPath);
        var root = document.RootElement;

        Assert.False(root.GetProperty("runtimeBehaviorChanged").GetBoolean());
        Assert.False(root.GetProperty("authorizationSemanticsChanged").GetBoolean());
        Assert.False(root.GetProperty("publicApiChanged").GetBoolean());
        Assert.False(root.GetProperty("calculationPhysicsChanged").GetBoolean());

        Assert.NotEmpty(root.GetProperty("characterizedDecisionCases").EnumerateArray());
    }

    [Fact]
    public void CharacterizationCapabilitiesIncludeRequiredCoverage()
    {
        using var document = GovernanceJsonTestHelper.Parse(CharacterizationJsonPath);
        var capabilities = document.RootElement.GetProperty("characterizedDecisionCases")
            .EnumerateArray()
            .Select(item => item.GetProperty("capability").GetString() ?? string.Empty)
            .ToArray();

        var required = new[]
        {
            "ProjectsRead",
            "ProjectsWrite",
            "BuildingsRead",
            "BuildingsWrite",
            "WorkflowsRead",
            "WorkflowsExecute",
            "CalculationRun",
            "ReportsRead",
            "ReportsWrite",
            "ArtifactRead"
        };

        foreach (var capability in required)
        {
            Assert.Contains(capabilities, value => value.Contains(capability, StringComparison.Ordinal));
        }
    }

    [Fact]
    public void CharacterizationNonClaimsArePresent()
    {
        using var document = GovernanceJsonTestHelper.Parse(CharacterizationJsonPath);
        var nonClaims = document.RootElement.GetProperty("nonClaims")
            .EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .ToArray();

        Assert.Contains(nonClaims, value => value.Contains("No authorization behavior change claim", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(nonClaims, value => value.Contains("No public API route change claim", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(nonClaims, value => value.Contains("No DTO shape change claim", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(nonClaims, value => value.Contains("No calculation physics change claim", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(nonClaims, value => value.Contains("No ownership backfill execution claim", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(nonClaims, value => value.Contains("No global EF query filter claim", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(nonClaims, value => value.Contains("No DB RLS claim", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(nonClaims, value => value.Contains("No production security certification claim", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void P8AuditKeepsAuthorizationGateCharacterizationProgressVisible()
    {
        using var audit = GovernanceJsonTestHelper.Parse(
            Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "engineering-domain-architecture-audit.json"));

        var findings = audit.RootElement.GetProperty("findings").EnumerateArray().ToArray();
        var gateFinding = findings.First(item => string.Equals(item.GetProperty("id").GetString(), "P8-00-F03", StringComparison.Ordinal));

        Assert.Contains(
            gateFinding.GetProperty("resolutionStatus").GetString(),
            new[] { "Characterized", "PartiallyAddressed" });
        Assert.Contains(
            gateFinding.GetProperty("resolutionStage").GetString(),
            new[] { "P8-03A", "P8-03C" });
    }

    [Fact]
    public void SecurityInventoryAndGuardrailsIncludeP8_03ACharacterizationStage()
    {
        using var inventory = GovernanceJsonTestHelper.Parse(
            GovernancePathHelper.SecurityDocPath("production-saas-readiness-inventory.json"));
        var inventoryItems = inventory.RootElement.GetProperty("p5Roadmap")
            .EnumerateArray()
            .Select(item => item.GetProperty("item").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);
        Assert.Contains("P8-03A", inventoryItems);

        using var guardrails = GovernanceJsonTestHelper.Parse(
            GovernancePathHelper.SecurityDocPath("security-regression-guardrails.json"));
        var guardrailIds = guardrails.RootElement.GetProperty("guardrails")
            .EnumerateArray()
            .Select(item => item.GetProperty("guardrailId").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);
        Assert.Contains("SEC-GUARD-PROTECTED-ENDPOINT-AUTHORIZATION-GATE-CHARACTERIZATION", guardrailIds);
    }

    private static string CharacterizationMarkdownPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "protected-endpoint-authorization-gate-characterization.md");

    private static string CharacterizationJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "protected-endpoint-authorization-gate-characterization.json");

    private static string CharacterizationSchemaPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "protected-endpoint-authorization-gate-characterization.schema.json");
}
