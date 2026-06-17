using System.Text.Json;
using System.Text.RegularExpressions;

namespace AssistantEngineer.Tests.EquipmentDiagnostics;

public class EquipmentDiagnosticsTelegramReleaseEvidenceTests
{
    private static readonly string ScriptPath = Path.Combine(
        TestPaths.RepoRoot, "scripts", "equipment-diagnostics", "prepare-telegram-closed-beta-release-evidence.ps1");
    private static readonly string DocPath = Path.Combine(
        TestPaths.RepoRoot, "docs", "equipment-diagnostics", "telegram-closed-beta-release-evidence.md");

    [Fact]
    public void ReleaseEvidenceDocAndScriptExistAndAreInventoried()
    {
        Assert.True(File.Exists(DocPath));
        Assert.True(File.Exists(ScriptPath));
        using var inventory = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            TestPaths.RepoRoot, "docs", "architecture", "scripts-tools-inventory.json")));
        Assert.Contains(inventory.RootElement.GetProperty("entries").EnumerateArray(), entry =>
            string.Equals(
                entry.GetProperty("path").GetString(),
                "scripts/equipment-diagnostics/prepare-telegram-closed-beta-release-evidence.ps1",
                StringComparison.Ordinal));
    }

    [Fact]
    public void ScriptProducesExpectedIgnoredEvidenceAndUsesGoalRunValidator()
    {
        var script = File.ReadAllText(ScriptPath);
        Assert.Contains("artifacts/verification/equipment-diagnostics/telegram-closed-beta", script, StringComparison.Ordinal);
        Assert.Contains("release-evidence-summary.md", script, StringComparison.Ordinal);
        Assert.Contains("release-evidence-report.json", script, StringComparison.Ordinal);
        Assert.Contains("telegram-closed-beta-goal-run-report.json", script, StringComparison.Ordinal);
        Assert.Contains("goal-run-report", script, StringComparison.Ordinal);
        Assert.Contains("OutputRoot must remain under artifacts/verification", script, StringComparison.Ordinal);
        Assert.Contains("/artifacts/", File.ReadAllText(Path.Combine(TestPaths.RepoRoot, ".gitignore")), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ScriptDoesNotRunEngineeringCoreVerifyOrContainRealCredentialsDomainsOrNetworkCalls()
    {
        var script = File.ReadAllText(ScriptPath);
        Assert.DoesNotContain("scripts/engineering-core/verify-engineering-core-v1.ps1", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotMatch(@"\b\d{8,10}:[A-Za-z0-9_-]{30,}\b", script);
        var withoutFrameworkNamespaces = script.Replace("System.IO", string.Empty, StringComparison.Ordinal);
        Assert.Empty(Regex.Matches(withoutFrameworkNamespaces, @"(?i)\b(?:[a-z0-9-]+\.)+(?:com|net|org|io|dev|cloud)\b"));
        Assert.DoesNotContain("Invoke-WebRequest", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Invoke-RestMethod", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotMatch(@"(?im)^\s*(?:&\s*)?docker\b", script);
    }

    [Fact]
    public void DocumentationContainsRequiredLimitationsAndSafetyStatements()
    {
        var docs = File.ReadAllText(DocPath);
        foreach (var fragment in new[]
                 {
                     "Closed beta only",
                     "not production or public release",
                     "No real secrets in Git",
                     "Telegram is disabled by default",
                     "Chat ID discovery is disabled by default",
                     "Polling disabled by default",
                     "No database or audit persistence",
                     "No external monitoring",
                     "Runtime catalog is the only final-answer source",
                     "are not final diagnosis",
                     "Vendor manual coverage remains partial",
                     "Generated artifacts are not committed",
                     "ED-20A",
                     "ED-21B"
                 })
        {
            Assert.Contains(fragment, docs, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void DocumentationContainsNoForbiddenClaims()
    {
        var docs = File.ReadAllText(DocPath);
        var forbidden = new[]
        {
            string.Concat("production ", "ready"),
            string.Concat("public release ", "ready"),
            string.Concat("fully autonomous ", "engineer"),
            string.Concat("autonomous production ", "execution"),
            string.Concat("AI ", "diagnosis"),
            string.Concat("RAG ", "diagnosis"),
            string.Concat("vector search ", "diagnosis"),
            string.Concat("full vendor manual ", "coverage"),
            string.Concat("full ", "parity"),
            string.Concat("ManualVerified ", "promotion")
        };
        foreach (var claim in forbidden)
        {
            Assert.DoesNotContain(claim, docs, StringComparison.OrdinalIgnoreCase);
        }
    }
}
