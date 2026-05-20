using AssistantEngineer.Tests.Architecture.Governance;

namespace AssistantEngineer.Tests.Architecture;

public sealed class P7SecurityDocsMapTests
{
    [Fact]
    public void SecurityDocsMapArtifactsExist()
    {
        GovernanceDocumentTestHelper.AssertFilesExist(
            SecurityDocsMapDocPath,
            SecurityDocsMapJsonPath,
            SecurityDocsMapSchemaPath);
    }

    [Fact]
    public void SecurityDocsMapDocumentContainsRequiredSections()
    {
        GovernanceDocumentTestHelper.AssertMarkdownContainsSections(
            SecurityDocsMapDocPath,
            [
                "## Purpose",
                "## Scope",
                "## Non-claims",
                "## How to read this map",
                "## Canonical documents",
                "## Stage evidence documents",
                "## Templates",
                "## Inventories and machine-readable registries",
                "## Guardrails and tests",
                "## Route protection documentation map",
                "## Tenant isolation documentation map",
                "## Ownership backfill documentation map",
                "## Apply governance documentation map",
                "## CI/release-ready documentation map",
                "## Decision records",
                "## Known documentation limitations",
                "## Next steps"
            ]);

        GovernanceDocumentTestHelper.AssertMarkdownContainsPhrases(
            SecurityDocsMapDocPath,
            [
                "No production security certification claim.",
                "No full multi-tenant isolation claim yet.",
                "No database row-level security claim.",
                "No global EF query filter claim.",
                "No production apply enabled claim.",
                "No ownership backfill execution claim."
            ]);
    }

    [Fact]
    public void SecurityDocsMapJsonParsesAndReferencesRequiredArtifacts()
    {
        using var document = GovernanceJsonTestHelper.Parse(SecurityDocsMapJsonPath);
        var root = document.RootElement;

        var canonical = root.GetProperty("canonicalDocuments").EnumerateArray().ToArray();
        var canonicalPaths = canonical
            .Select(item => item.GetProperty("path").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);
        Assert.Contains("docs/security/security-release-boundary.md", canonicalPaths);
        Assert.Contains("docs/security/security-governance-index.md", canonicalPaths);
        Assert.Contains("docs/adr/security-architecture-decision-matrix.md", canonicalPaths);

        var stageEvidencePaths = root.GetProperty("stageEvidenceDocuments").EnumerateArray()
            .Select(item => item.GetProperty("path").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);
        Assert.Contains("docs/adr/future-security-adr-backlog.md", stageEvidencePaths);

        var registries = GovernanceJsonTestHelper.StringSet(root.GetProperty("machineReadableRegistries"));
        Assert.Contains("docs/security/api-endpoint-protection-inventory.json", registries);

        var tests = GovernanceJsonTestHelper.StringSet(root.GetProperty("guardrailTests"));
        Assert.Contains("P7RouteInventoryCoverageTests", tests);
        Assert.Contains("P7SecurityDocsMapTests", tests);
        Assert.Contains("P7SecurityAdrTests", tests);
        Assert.Contains("P7SecurityArchitectureDecisionMatrixTests", tests);
        Assert.Contains("P7FutureSecurityAdrBacklogTests", tests);

        Assert.NotEmpty(root.GetProperty("nonClaims").EnumerateArray());

        foreach (var entry in canonical)
            AssertPathExists(entry.GetProperty("path").GetString() ?? string.Empty);

        foreach (var entry in root.GetProperty("stageEvidenceDocuments").EnumerateArray())
            AssertPathExists(entry.GetProperty("path").GetString() ?? string.Empty);

        foreach (var entry in root.GetProperty("templateDocuments").EnumerateArray())
            AssertPathExists(entry.GetProperty("path").GetString() ?? string.Empty);

        foreach (var registry in registries)
            AssertPathExists(registry);
    }

    [Fact]
    public void InventoryAndGuardrailsContainP7_07Signals()
    {
        using var inventory = GovernanceJsonTestHelper.Parse(InventoryJsonPath);
        var roadmapItems = inventory.RootElement.GetProperty("p5Roadmap")
            .EnumerateArray()
            .Select(item => item.GetProperty("item").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);
        Assert.Contains("P7-07", roadmapItems);
        Assert.Contains("P7-08", roadmapItems);

        using var guardrails = GovernanceJsonTestHelper.Parse(GuardrailsJsonPath);
        var guardrailIds = guardrails.RootElement.GetProperty("guardrails")
            .EnumerateArray()
            .Select(item => item.GetProperty("guardrailId").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);
        Assert.Contains("SEC-GUARD-SECURITY-DOCS-MAP-ADR", guardrailIds);
        Assert.Contains("SEC-GUARD-SECURITY-ADR-DECISION-MATRIX", guardrailIds);
    }

    private static void AssertPathExists(string repoRelativePath)
    {
        Assert.False(string.IsNullOrWhiteSpace(repoRelativePath));
        var absolute = Path.Combine(TestPaths.RepoRoot, repoRelativePath.Replace('/', Path.DirectorySeparatorChar));
        Assert.True(File.Exists(absolute), $"Referenced path was not found: {repoRelativePath}");
    }

    private static string SecurityDocsMapDocPath =>
        GovernancePathHelper.SecurityDocPath("security-docs-map.md");

    private static string SecurityDocsMapJsonPath =>
        GovernancePathHelper.SecurityDocPath("security-docs-map.json");

    private static string SecurityDocsMapSchemaPath =>
        GovernancePathHelper.SecurityDocPath("security-docs-map.schema.json");

    private static string InventoryJsonPath =>
        GovernancePathHelper.SecurityDocPath("production-saas-readiness-inventory.json");

    private static string GuardrailsJsonPath =>
        GovernancePathHelper.SecurityDocPath("security-regression-guardrails.json");
}
