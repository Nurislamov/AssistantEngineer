using System.Text.Json;
using AssistantEngineer.Tests;

namespace AssistantEngineer.Tests.Architecture;

public sealed class P7ReleaseReadyObservabilityAuditTests
{
    [Fact]
    public void AuditArtifactsExist()
    {
        Assert.True(File.Exists(AuditDocPath));
        Assert.True(File.Exists(AuditJsonPath));
        Assert.True(File.Exists(AuditSchemaPath));
    }

    [Fact]
    public void AuditDocumentContainsRequiredSectionsAndNonClaims()
    {
        var content = File.ReadAllText(AuditDocPath);

        var requiredSections = new[]
        {
            "## Purpose",
            "## Scope",
            "## Non-claims",
            "## Current release-ready flow",
            "## Observability gaps",
            "## Performance hotspots",
            "## Timeout risks",
            "## Safe diagnostics policy",
            "## Proposed improvements",
            "## Implemented improvements",
            "## Remaining limitations",
            "## Next steps"
        };

        foreach (var section in requiredSections)
            Assert.Contains(section, content, StringComparison.Ordinal);

        Assert.Contains("No production security certification claim.", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("No production apply enabled claim.", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AuditJsonFlagsRemainFalseAndParse()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(AuditJsonPath));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("runtimeBehaviorChanged").GetBoolean());
        Assert.False(root.GetProperty("releaseGateSemanticsChanged").GetBoolean());
        Assert.True(root.GetProperty("observabilityImprovements").GetArrayLength() > 0);
    }

    [Fact]
    public void InventoryGuardrailsAndIndexReferenceP7_04()
    {
        var inventory = File.ReadAllText(Path.Combine(TestPaths.RepoRoot, "docs", "security", "production-saas-readiness-inventory.json"));
        Assert.Contains("\"item\":  \"P7-04\"", inventory, StringComparison.Ordinal);

        var guardrails = File.ReadAllText(Path.Combine(TestPaths.RepoRoot, "docs", "security", "security-regression-guardrails.json"));
        Assert.Contains("SEC-GUARD-RELEASE-READY-OBSERVABILITY", guardrails, StringComparison.Ordinal);
        Assert.Contains("ReleaseReadyScriptObservabilityTests", guardrails, StringComparison.Ordinal);

        var index = File.ReadAllText(Path.Combine(TestPaths.RepoRoot, "docs", "security", "security-governance-index.json"));
        Assert.Contains("release-ready-observability-audit.md", index, StringComparison.Ordinal);
    }

    [Fact]
    public void AuditDoesNotClaimProductionCertificationOrBackfillExecution()
    {
        var content = File.ReadAllText(AuditDocPath);
        Assert.DoesNotContain("production apply is enabled", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ownership backfill was executed", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("full tenant isolation complete", content, StringComparison.OrdinalIgnoreCase);
    }

    private static string AuditDocPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "release-ready-observability-audit.md");

    private static string AuditJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "release-ready-observability-audit.json");

    private static string AuditSchemaPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "release-ready-observability-audit.schema.json");
}
