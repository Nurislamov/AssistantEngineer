using AssistantEngineer.Tests.Architecture.Governance;

namespace AssistantEngineer.Tests.Architecture;

public sealed class P8AuthorizationWorkflowHotspotInventoryTests
{
    [Fact]
    public void HotspotInventoryArtifactsExist()
    {
        GovernanceDocumentTestHelper.AssertFilesExist(
            InventoryMarkdownPath,
            InventoryJsonPath,
            InventorySchemaPath);
    }

    [Fact]
    public void HotspotInventoryJsonParsesAndFlagsRemainFalse()
    {
        using var document = GovernanceJsonTestHelper.Parse(InventoryJsonPath);
        var root = document.RootElement;

        Assert.False(root.GetProperty("runtimeBehaviorChanged").GetBoolean());
        Assert.False(root.GetProperty("authorizationSemanticsChanged").GetBoolean());
        Assert.False(root.GetProperty("publicApiChanged").GetBoolean());
        Assert.False(root.GetProperty("calculationPhysicsChanged").GetBoolean());

        var hotspots = root.GetProperty("hotspots").EnumerateArray().ToArray();
        Assert.Contains(
            hotspots,
            item => string.Equals(item.GetProperty("id").GetString(), "P8-03-HOTSPOT-AUTH-GATE", StringComparison.Ordinal));
        Assert.Contains(
            hotspots,
            item => string.Equals(item.GetProperty("id").GetString(), "P8-03-HOTSPOT-WORKFLOW-CONTROLLER", StringComparison.Ordinal));

        var nonClaims = root.GetProperty("nonClaims")
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
    public void P8AuditContainsP8_03StatusForAuthorizationAndWorkflowHotspots()
    {
        using var audit = GovernanceJsonTestHelper.Parse(
            Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "engineering-domain-architecture-audit.json"));

        var findings = audit.RootElement.GetProperty("findings").EnumerateArray().ToArray();

        var gateFinding = findings.First(item => string.Equals(item.GetProperty("id").GetString(), "P8-00-F03", StringComparison.Ordinal));
        Assert.Contains(
            gateFinding.GetProperty("resolutionStatus").GetString(),
            new[] { "InProgress", "DesignReady", "Characterized", "PartiallyAddressed" });
        Assert.Contains(
            gateFinding.GetProperty("resolutionStage").GetString(),
            new[] { "P8-03", "P8-03A", "P8-03C" });

        var workflowFinding = findings.First(item => string.Equals(item.GetProperty("id").GetString(), "P8-00-F08", StringComparison.Ordinal));
        Assert.Contains(workflowFinding.GetProperty("resolutionStatus").GetString(), new[] { "InProgress", "DesignReady", "Characterized", "PartiallyAddressed" });
        Assert.Contains(workflowFinding.GetProperty("resolutionStage").GetString(), new[] { "P8-03", "P8-03D", "P8-03E", "P8-03F" });
    }

    private static string InventoryMarkdownPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "authorization-workflow-hotspot-inventory.md");

    private static string InventoryJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "authorization-workflow-hotspot-inventory.json");

    private static string InventorySchemaPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "authorization-workflow-hotspot-inventory.schema.json");
}
