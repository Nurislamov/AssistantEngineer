using AssistantEngineer.Tests.Architecture.Governance;

namespace AssistantEngineer.Tests.Architecture;

public sealed class P7RouteInventoryClaimsConsistencyAuditTests
{
    [Fact]
    public void AuditArtifactsExist()
    {
        GovernanceDocumentTestHelper.AssertFilesExist(
            AuditDocPath,
            AuditJsonPath,
            AuditSchemaPath,
            ClassificationModelDocPath);
    }

    [Fact]
    public void AuditDocumentContainsRequiredSectionsAndNonClaims()
    {
        GovernanceDocumentTestHelper.AssertMarkdownContainsSections(
            AuditDocPath,
            [
                "## Purpose",
                "## Scope",
                "## Non-claims",
                "## Current route inventory sources",
                "## Endpoint classification model",
                "## Protection stage mapping",
                "## Claims consistency rules",
                "## Release boundary relationship",
                "## Automation gaps",
                "## Implemented automation",
                "## Remaining limitations",
                "## Next steps"
            ]);

        GovernanceDocumentTestHelper.AssertMarkdownContainsPhrases(
            AuditDocPath,
            [
                "No production security certification claim.",
                "No full multi-tenant isolation claim yet.",
                "No database row-level security claim.",
                "No global EF query filter claim.",
                "No production apply enabled claim."
            ]);
    }

    [Fact]
    public void AuditJsonFlagsRemainFalse()
    {
        using var doc = GovernanceJsonTestHelper.Parse(AuditJsonPath);
        var root = doc.RootElement;
        Assert.False(root.GetProperty("runtimeBehaviorChanged").GetBoolean());
        Assert.False(root.GetProperty("publicRoutesChanged").GetBoolean());
        Assert.False(root.GetProperty("routeProtectionWeakened").GetBoolean());
    }

    [Fact]
    public void InventoryGuardrailsAndIndexContainP7_06()
    {
        using var inventory = GovernanceJsonTestHelper.Parse(InventoryJsonPath);
        var items = inventory.RootElement.GetProperty("p5Roadmap")
            .EnumerateArray()
            .Select(item => item.GetProperty("item").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);
        Assert.Contains("P7-06", items);

        using var guardrails = GovernanceJsonTestHelper.Parse(GuardrailsJsonPath);
        var guardrailIds = guardrails.RootElement.GetProperty("guardrails")
            .EnumerateArray()
            .Select(item => item.GetProperty("guardrailId").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);
        Assert.Contains("SEC-GUARD-ROUTE-INVENTORY-CLAIMS-CONSISTENCY", guardrailIds);

        using var index = GovernanceJsonTestHelper.Parse(IndexJsonPath);
        var paths = index.RootElement.GetProperty("documents")
            .EnumerateArray()
            .Select(item => item.GetProperty("path").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);
        Assert.Contains("docs/security/route-inventory-claims-consistency-audit.md", paths);
        Assert.Contains("docs/security/api-endpoint-classification-model.md", paths);
    }

    private static string AuditDocPath =>
        GovernancePathHelper.SecurityDocPath("route-inventory-claims-consistency-audit.md");

    private static string AuditJsonPath =>
        GovernancePathHelper.SecurityDocPath("route-inventory-claims-consistency-audit.json");

    private static string AuditSchemaPath =>
        GovernancePathHelper.SecurityDocPath("route-inventory-claims-consistency-audit.schema.json");

    private static string ClassificationModelDocPath =>
        GovernancePathHelper.SecurityDocPath("api-endpoint-classification-model.md");

    private static string InventoryJsonPath =>
        GovernancePathHelper.SecurityDocPath("production-saas-readiness-inventory.json");

    private static string GuardrailsJsonPath =>
        GovernancePathHelper.SecurityDocPath("security-regression-guardrails.json");

    private static string IndexJsonPath =>
        GovernancePathHelper.SecurityDocPath("security-governance-index.json");
}
