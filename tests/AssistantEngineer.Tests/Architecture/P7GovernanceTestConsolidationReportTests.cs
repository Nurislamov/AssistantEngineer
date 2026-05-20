using System.Text.Json;
using AssistantEngineer.Tests.Architecture.Governance;

namespace AssistantEngineer.Tests.Architecture;

public sealed class P7GovernanceTestConsolidationReportTests
{
    [Fact]
    public void ConsolidationReportArtifactsExist()
    {
        GovernanceDocumentTestHelper.AssertFilesExist(
            ConsolidationReportDocPath,
            ConsolidationReportJsonPath,
            ConsolidationReportSchemaPath);
    }

    [Fact]
    public void ConsolidationReportDocContainsRequiredSections()
    {
        GovernanceDocumentTestHelper.AssertMarkdownContainsSections(
            ConsolidationReportDocPath,
            [
                "## Purpose",
                "## Scope",
                "## Non-claims",
                "## Consolidated helper areas",
                "## Refactored test classes",
                "## Preserved guardrails",
                "## Remaining duplication",
                "## Risk assessment",
                "## Next steps"
            ]);
    }

    [Fact]
    public void ConsolidationReportJsonParsesAndKeepsRuntimeAndWritePathDisabled()
    {
        using var document = GovernanceJsonTestHelper.Parse(ConsolidationReportJsonPath);
        var root = document.RootElement;

        Assert.False(root.GetProperty("runtimeBehaviorChanged").GetBoolean());
        Assert.False(root.GetProperty("writePathEnabled").GetBoolean());

        var helpers = root.GetProperty("helpersAdded").EnumerateArray().ToArray();
        Assert.NotEmpty(helpers);

        var preserved = root.GetProperty("preservedGuardrails").EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("ApplyDisabledBoundary", preserved);
        Assert.Contains("ForbiddenClaims", preserved);
        Assert.Contains("ReleaseBoundary", preserved);
        Assert.Contains("GeneratedArtifactsIgnored", preserved);
        Assert.Contains("NoDestructiveSql", preserved);
        Assert.Contains("NoGlobalEfQueryFilters", preserved);
    }

    [Fact]
    public void InventoryAndGuardrailsContainP7_02References()
    {
        using var inventory = GovernanceJsonTestHelper.Parse(InventoryJsonPath);
        var items = inventory.RootElement.GetProperty("p5Roadmap")
            .EnumerateArray()
            .Select(item => item.GetProperty("item").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);
        Assert.Contains("P7-02", items);

        using var guardrails = GovernanceJsonTestHelper.Parse(GuardrailsJsonPath);
        var guardrailIds = guardrails.RootElement.GetProperty("guardrails")
            .EnumerateArray()
            .Select(item => item.GetProperty("guardrailId").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);
        Assert.Contains("SEC-GUARD-GOVERNANCE-TEST-CONSOLIDATION", guardrailIds);
    }

    private static string ConsolidationReportDocPath =>
        GovernancePathHelper.SecurityDocPath("governance-test-consolidation-report.md");

    private static string ConsolidationReportJsonPath =>
        GovernancePathHelper.SecurityDocPath("governance-test-consolidation-report.json");

    private static string ConsolidationReportSchemaPath =>
        GovernancePathHelper.SecurityDocPath("governance-test-consolidation-report.schema.json");

    private static string InventoryJsonPath =>
        GovernancePathHelper.SecurityDocPath("production-saas-readiness-inventory.json");

    private static string GuardrailsJsonPath =>
        GovernancePathHelper.SecurityDocPath("security-regression-guardrails.json");
}
