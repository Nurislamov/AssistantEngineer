using AssistantEngineer.Tests;

namespace AssistantEngineer.Tests.Architecture;

public class ArchitectureHardeningDocumentationTests
{
    [Fact]
    public void LegacyInventoryDoc_Exists_AndContainsRequiredSections()
    {
        Assert.True(File.Exists(LegacyInventoryPath), $"Legacy inventory document is missing: {LegacyInventoryPath}");

        var content = File.ReadAllText(LegacyInventoryPath);
        var requiredSections = new[]
        {
            "## Active calculation path",
            "## Compatibility / legacy path",
            "## Deprecated candidates",
            "## Do not remove yet",
            "## Migration notes",
            "## Risk notes"
        };

        foreach (var requiredSection in requiredSections)
        {
            Assert.Contains(requiredSection, content, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void ArchitectureHardeningReport_Exists_AndContainsRequiredScope()
    {
        Assert.True(File.Exists(HardeningReportPath), $"Architecture hardening report is missing: {HardeningReportPath}");

        var content = File.ReadAllText(HardeningReportPath);
        var requiredPhrases = new[]
        {
            "EnergyCalculationPipelineService",
            "BuildingWorkspace.tsx",
            "tools/*.Program.cs",
            "PowerShell scripts",
            "Legacy services found",
            "Guardrails added in this pass",
            "Remaining risks",
            "No EnergyPlus parity claim.",
            "No pyBuildingEnergy parity claim.",
            "No ASHRAE 140 validation claim.",
            "No full ISO/EN compliance claim.",
            "Validation anchors remain validation anchors only.",
            "infrastructure readiness"
        };

        foreach (var requiredPhrase in requiredPhrases)
        {
            Assert.Contains(requiredPhrase, content, StringComparison.Ordinal);
        }

        var requiredFinalAuditSections = new[]
        {
            "## Final hardening audit",
            "### Checks performed",
            "### Passed checks",
            "### Manifest/governance verification",
            "### Backend guard status",
            "### Frontend guard status",
            "### Tools guard status",
            "### Legacy enforcement status",
            "### Remaining risks",
            "### Recommended next phase"
        };

        foreach (var requiredSection in requiredFinalAuditSections)
        {
            Assert.Contains(requiredSection, content, StringComparison.Ordinal);
        }

        var requiredPhaseTwoSections = new[]
        {
            "## Engineering Core Hardening Phase 2",
            "### Frontend gate status",
            "### Governance generation idempotency",
            "### Backend pipeline extraction phase 2",
            "### Frontend workspace decomposition phase 2",
            "### Legacy enforcement",
            "### Verification results",
            "### Remaining risks",
            "### Recommended next phase"
        };

        foreach (var requiredSection in requiredPhaseTwoSections)
        {
            Assert.Contains(requiredSection, content, StringComparison.Ordinal);
        }
    }

    private static string LegacyInventoryPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "calculation-legacy-inventory.md");

    private static string HardeningReportPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "architecture-hardening-report.md");
}
