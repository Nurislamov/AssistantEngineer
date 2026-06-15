using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AssistantEngineer.Tests.EquipmentDiagnostics;

public class EquipmentDiagnosticsTelegramReleaseTagHandoffTests
{
    private static readonly string ScriptPath = Path.Combine(
        TestPaths.RepoRoot, "scripts", "equipment-diagnostics", "prepare-telegram-closed-beta-release-tag-handoff.ps1");
    private static readonly string[] DocumentPaths =
    [
        Path.Combine(TestPaths.RepoRoot, "docs", "equipment-diagnostics", "telegram-closed-beta-release-tag-and-handoff.md"),
        Path.Combine(TestPaths.RepoRoot, "docs", "equipment-diagnostics", "telegram-closed-beta-release-notes-template.md")
    ];

    [Fact]
    public void ReleaseTagHandoffDocumentsAndScriptExistAndAreInventoried()
    {
        Assert.All(DocumentPaths, path => Assert.True(File.Exists(path), path));
        Assert.True(File.Exists(ScriptPath));
        using var inventory = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            TestPaths.RepoRoot, "docs", "architecture", "scripts-tools-inventory.json")));
        Assert.Contains(inventory.RootElement.GetProperty("entries").EnumerateArray(), entry =>
            string.Equals(entry.GetProperty("path").GetString(),
                "scripts/equipment-diagnostics/prepare-telegram-closed-beta-release-tag-handoff.ps1",
                StringComparison.Ordinal));
    }

    [Fact]
    public void ScriptDeclaresExpectedOutputsAndPriorEvidenceRunners()
    {
        var script = File.ReadAllText(ScriptPath);
        AssertContainsAll(script,
            "artifacts/verification/equipment-diagnostics/telegram-release-tag-handoff",
            "release-tag-handoff-summary.md",
            "release-tag-handoff-report.json",
            "prepare-telegram-closed-beta-final-go-no-go.ps1",
            "prepare-telegram-closed-beta-activation-checklist.ps1",
            "prepare-telegram-closed-beta-deployment-dry-run.ps1",
            "prepare-telegram-closed-beta-release-evidence.ps1",
            "verify-branch-readiness.ps1",
            "set-telegram-webhook.ps1",
            "get-telegram-webhook-info.ps1",
            "delete-telegram-webhook.ps1");
        Assert.DoesNotContain("scripts/engineering-core/verify-engineering-core-v1.ps1", script, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ScriptDoesNotInvokeNetworkWebhookOrGitTagMutation()
    {
        var script = File.ReadAllText(ScriptPath);
        Assert.DoesNotContain("Invoke-WebRequest", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Invoke-RestMethod", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("api.telegram.org", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotMatch(@"(?im)^\s*(?:&\s*)?\.\\scripts\\equipment-diagnostics\\(?:set|get|delete)-telegram-webhook", script);
        Assert.DoesNotMatch(@"(?im)Get-Content[^\r\n]*deploy[/\\]\.env(?:\s|[""'])", script);
        Assert.DoesNotMatch(@"(?im)^\s*(?:&\s*)?git\s+tag\b", script);
        Assert.DoesNotMatch(@"(?im)^\s*(?:&\s*)?git\s+push\b", script);
    }

    [Fact]
    public void ScriptHandlesDetachedHeadAndValidatesReleaseTagFormat()
    {
        var script = File.ReadAllText(ScriptPath);
        AssertContainsAll(script,
            "function Invoke-GitText",
            "GITHUB_HEAD_REF",
            "GITHUB_REF_NAME",
            "$branch = \"detached\"",
            "$head = Invoke-GitText",
            "$resolvedBase = Invoke-GitText",
            "equipment-diagnostics-telegram-closed-beta-v\\d+\\.\\d+\\.\\d+");
    }

    [Fact]
    public void DocumentsContainRequiredSafetyAndReleaseStatements()
    {
        var docs = ReadDocuments();
        AssertContainsAll(docs,
            "closed beta only",
            "not for production or public launch",
            "no real secrets in Git",
            "no real domains in Git",
            "no real chat IDs in Git",
            "Telegram disabled by default",
            "chat ID discovery disabled by default",
            "no long polling",
            "no DB/audit persistence",
            "no external monitoring",
            "runtime catalog remains the only final-answer source",
            "manual-codebook/staging/preview are not final diagnosis",
            "do not claim complete vendor manual coverage",
            "do not bypass protections",
            "no hazardous electrical/refrigerant instructions",
            "tag/handoff does not call Telegram network",
            "tag/handoff does not execute setWebhook/getWebhookInfo/deleteWebhook",
            "real activation happens only in ED-23A or a manually approved operational window",
            "equipment-diagnostics-telegram-closed-beta-v0.1.0");
    }

    [Fact]
    public void DocumentsAndScriptContainNoForbiddenClaimsOrSensitiveExamples()
    {
        var combined = ReadDocuments() + Environment.NewLine + File.ReadAllText(ScriptPath);
        foreach (var claim in ForbiddenClaims)
        {
            Assert.DoesNotContain(claim, combined, StringComparison.OrdinalIgnoreCase);
        }

        Assert.DoesNotMatch(@"\b\d{8,10}:[A-Za-z0-9_-]{30,}\b", combined);
        Assert.DoesNotMatch(@"(?i)\bchat\s*id\s*[:=]\s*-?\d+\b", combined);
        Assert.DoesNotContain(
            Regex.Matches(combined, @"(?i)https?://(?<host>[a-z0-9.-]+)"),
            match =>
            {
                var host = match.Groups["host"].Value;
                return !host.Equals("localhost", StringComparison.OrdinalIgnoreCase) &&
                       !host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase) &&
                       !host.EndsWith("example.com", StringComparison.OrdinalIgnoreCase) &&
                       !host.EndsWith("example.test", StringComparison.OrdinalIgnoreCase);
            });
    }

    [Fact]
    public void FocusedReleaseTagHandoffCreatesReportsAndExitsZero()
    {
        var outputRoot = Path.Combine(
            "artifacts", "verification", "equipment-diagnostics",
            "telegram-release-tag-handoff-test", Guid.NewGuid().ToString("N"));
        var fullOutputRoot = Path.Combine(TestPaths.RepoRoot, outputRoot);

        try
        {
            var result = RunPowerShell(
                ".\\scripts\\equipment-diagnostics\\prepare-telegram-closed-beta-release-tag-handoff.ps1 " +
                $"-BaseRef origin/master -OutputRoot \"{outputRoot}\" " +
                "-SkipFinalGoNoGoReference -SkipBranchReadiness -SkipBackendTests -SkipEquipmentDiagnosticsTests");

            Assert.True(result.ExitCode == 0, result.Output);
            Assert.Contains("Status: PASS", result.Output, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Release tag: equipment-diagnostics-telegram-closed-beta-v0.1.0", result.Output, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Decision: READY_WITH_MANUAL_REVIEW", result.Output, StringComparison.OrdinalIgnoreCase);
            Assert.True(File.Exists(Path.Combine(fullOutputRoot, "release-tag-handoff-summary.md")));
            Assert.True(File.Exists(Path.Combine(fullOutputRoot, "release-tag-handoff-report.json")));
        }
        finally
        {
            if (Directory.Exists(fullOutputRoot))
            {
                Directory.Delete(fullOutputRoot, recursive: true);
            }
        }
    }

    private static readonly string[] ForbiddenClaims =
    [
        string.Concat("production ", "ready"),
        "production-ready",
        string.Concat("public release ", "ready"),
        string.Concat("full vendor manual ", "coverage"),
        string.Concat("AI/RAG ", "enabled"),
        string.Concat("fully autonomous ", "engineer"),
        string.Concat("autonomous production ", "execution"),
        string.Concat("full ", "parity"),
        string.Concat("ManualVerified ", "promotion")
    ];

    private static string ReadDocuments() => string.Join(Environment.NewLine, DocumentPaths.Select(File.ReadAllText));

    private static void AssertContainsAll(string content, params string[] fragments)
    {
        foreach (var fragment in fragments)
        {
            Assert.Contains(fragment, content, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static (int ExitCode, string Output) RunPowerShell(string arguments)
    {
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = "powershell",
            Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{arguments}\"",
            WorkingDirectory = TestPaths.RepoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        })!;
        var output = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
        process.WaitForExit();
        return (process.ExitCode, output);
    }
}
