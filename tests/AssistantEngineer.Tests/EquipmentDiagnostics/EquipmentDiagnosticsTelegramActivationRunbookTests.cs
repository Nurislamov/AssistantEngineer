using System.Diagnostics;
using System.Text.RegularExpressions;

namespace AssistantEngineer.Tests.EquipmentDiagnostics;

public class EquipmentDiagnosticsTelegramActivationRunbookTests
{
    private static readonly string ScriptPath = Path.Combine(
        TestPaths.RepoRoot, "scripts", "equipment-diagnostics", "prepare-telegram-closed-beta-activation-checklist.ps1");
    private static readonly string[] DocumentPaths =
    [
        Path.Combine(TestPaths.RepoRoot, "docs", "equipment-diagnostics", "telegram-closed-beta-activation-runbook.md"),
        Path.Combine(TestPaths.RepoRoot, "docs", "equipment-diagnostics", "telegram-closed-beta-smoke-evidence-template.md")
    ];

    [Fact]
    public void ActivationDocumentsAndChecklistScriptExist()
    {
        Assert.All(DocumentPaths, path => Assert.True(File.Exists(path), path));
        Assert.True(File.Exists(ScriptPath));
    }

    [Fact]
    public void ScriptDeclaresExpectedOutputsAndOperationInventory()
    {
        var script = File.ReadAllText(ScriptPath);
        AssertContainsAll(script,
            "artifacts/verification/equipment-diagnostics/telegram-activation-checklist",
            "activation-checklist-summary.md",
            "activation-checklist-report.json",
            "set-telegram-webhook.ps1",
            "get-telegram-webhook-info.ps1",
            "delete-telegram-webhook.ps1");
    }

    [Fact]
    public void ScriptDoesNotInvokeTelegramNetworkEngineeringCoreOrPrivateEnvironment()
    {
        var script = File.ReadAllText(ScriptPath);
        Assert.DoesNotContain("Invoke-WebRequest", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Invoke-RestMethod", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotMatch(@"(?im)^\s*(?:&\s*)?\.\\scripts\\equipment-diagnostics\\(?:set|get|delete)-telegram-webhook", script);
        Assert.DoesNotContain("scripts/engineering-core/verify-engineering-core-v1.ps1", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotMatch(@"(?im)Get-Content[^\r\n]*deploy[/\\]\.env(?:\s|[""'])", script);
        Assert.DoesNotMatch(@"(?im)^\s*(?:&\s*)?docker\b", script);
    }

    [Fact]
    public void ActivationDocumentsContainRequiredSafetyStatements()
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
            "chat ID discovery may be enabled only temporarily during setup",
            "polling disabled by default",
            "no DB/audit persistence",
            "no external monitoring",
            "runtime catalog remains the only final-answer source",
            "manual-codebook/staging/preview are not final diagnosis",
            "do not claim complete vendor manual coverage",
            "do not bypass protections",
            "no hazardous electrical/refrigerant instructions");
    }

    [Fact]
    public void ActivationDocumentsAndScriptContainNoForbiddenClaimsOrSensitiveExamples()
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
    public void FocusedChecklistCreatesIgnoredReportsAndCleansUp()
    {
        var outputRoot = Path.Combine(
            "artifacts", "verification", "equipment-diagnostics",
            "telegram-activation-checklist-test", Guid.NewGuid().ToString("N"));
        var fullOutputRoot = Path.Combine(TestPaths.RepoRoot, outputRoot);

        try
        {
            var result = RunPowerShell(
                ".\\scripts\\equipment-diagnostics\\prepare-telegram-closed-beta-activation-checklist.ps1 " +
                $"-BaseRef origin/master -OutputRoot \"{outputRoot}\"");

            Assert.True(result.ExitCode == 0, result.Output);
            Assert.Contains("Status: PASS", result.Output, StringComparison.OrdinalIgnoreCase);
            Assert.True(File.Exists(Path.Combine(fullOutputRoot, "activation-checklist-summary.md")));
            Assert.True(File.Exists(Path.Combine(fullOutputRoot, "activation-checklist-report.json")));
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
