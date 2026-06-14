using System.Text.Json;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Verification;

namespace AssistantEngineer.Tests.EquipmentDiagnostics;

public class EquipmentDiagnosticsBetaReadinessReportTests
{
    [Fact]
    public void CurrentRepositoryProducesCompleteClosedBetaReportWithoutBlockers()
    {
        var report = new EquipmentDiagnosticsBetaReadinessReportGenerator().Generate(new(
            RepositoryRoot: TestPaths.RepoRoot,
            GeneratedAtUtc: DateTimeOffset.UnixEpoch));

        Assert.Equal(15, report.Sections.Count);
        Assert.Equal(0, report.BlockerCount);
        Assert.Equal(EquipmentDiagnosticsBetaReadinessStatus.Warning, report.OverallStatus);
        Assert.Contains(report.KnownLimitations, value => value.Contains("Closed beta only", StringComparison.Ordinal));
        Assert.Contains(report.KnownLimitations, value => value.Contains("runtime catalog is the only source", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(report.KnownLimitations, value => value.Contains("not final diagnosis", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(report.KnownLimitations, value => value.Contains("partial", StringComparison.OrdinalIgnoreCase));

        var json = JsonSerializer.Serialize(report);
        Assert.DoesNotContain(TestPaths.RepoRoot, json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("BotToken", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("WebhookSecret", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MissingRepositoryContractsProduceBlockers()
    {
        var root = Path.Combine(Path.GetTempPath(), $"assistant-engineer-beta-readiness-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            var report = new EquipmentDiagnosticsBetaReadinessReportGenerator().Generate(new(
                RepositoryRoot: root,
                GeneratedAtUtc: DateTimeOffset.UnixEpoch));

            Assert.Equal(EquipmentDiagnosticsBetaReadinessStatus.Blocker, report.OverallStatus);
            Assert.True(report.BlockerCount > 0);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void BetaReadinessToolingAndClosedBetaDocsRemainPresent()
    {
        var required = new[]
        {
            "scripts/equipment-diagnostics/prepare-beta-readiness-report.ps1",
            "docs/equipment-diagnostics/beta-readiness-report.md",
            "docs/equipment-diagnostics/closed-beta-operator-quickstart.md",
            "docs/equipment-diagnostics/closed-beta-release-checklist.md"
        };
        foreach (var path in required)
        {
            Assert.True(File.Exists(Path.Combine(TestPaths.RepoRoot, path.Replace('/', Path.DirectorySeparatorChar))), path);
        }

        var program = File.ReadAllText(Path.Combine(
            TestPaths.RepoRoot, "tools", "AssistantEngineer.Tools.EquipmentDiagnosticsVerification", "Program.cs"));
        var preparePr = File.ReadAllText(Path.Combine(TestPaths.RepoRoot, "scripts", "dev", "verify-and-prepare-pr.ps1"));
        var inventory = File.ReadAllText(Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "scripts-tools-inventory.json"));
        Assert.Contains("beta-readiness", program, StringComparison.Ordinal);
        Assert.Contains("prepare-beta-readiness-report.ps1", preparePr, StringComparison.Ordinal);
        Assert.Contains("prepare-beta-readiness-report.ps1", inventory, StringComparison.Ordinal);
    }

    [Fact]
    public void BranchReadinessBlocksGeneratedBetaReportsAndEngineeringCoreVerifyChanges()
    {
        var source = File.ReadAllText(Path.Combine(
            TestPaths.RepoRoot,
            "src", "Backend", "AssistantEngineer.Modules.EquipmentDiagnostics",
            "Application", "Verification", "BranchReadinessVerificationService.cs"));

        Assert.Contains("CommittedBetaReadinessArtifact", source, StringComparison.Ordinal);
        Assert.Contains("EngineeringCoreVerificationScriptChanged", source, StringComparison.Ordinal);
    }
}
