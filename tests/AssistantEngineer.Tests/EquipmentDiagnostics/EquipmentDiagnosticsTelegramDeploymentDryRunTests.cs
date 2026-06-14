using System.Diagnostics;
using System.Text.RegularExpressions;

namespace AssistantEngineer.Tests.EquipmentDiagnostics;

public class EquipmentDiagnosticsTelegramDeploymentDryRunTests
{
    private static readonly string ScriptPath = Path.Combine(
        TestPaths.RepoRoot, "scripts", "equipment-diagnostics", "prepare-telegram-closed-beta-deployment-dry-run.ps1");
    private static readonly string DocPath = Path.Combine(
        TestPaths.RepoRoot, "docs", "equipment-diagnostics", "telegram-closed-beta-deployment-dry-run.md");

    [Fact]
    public void DryRunDocAndScriptExist()
    {
        Assert.True(File.Exists(DocPath));
        Assert.True(File.Exists(ScriptPath));
    }

    [Fact]
    public void ScriptDeclaresExpectedOutputsAndRequiredDeploymentInventory()
    {
        var script = File.ReadAllText(ScriptPath);
        AssertContainsAll(script,
            "artifacts/verification/equipment-diagnostics/telegram-deployment-dry-run",
            "deployment-dry-run-summary.md",
            "deployment-dry-run-report.json",
            "deploy/docker-compose.yml",
            "deploy/.env.example",
            "deploy/reverse-proxy/Caddyfile.example",
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
        Assert.DoesNotMatch(@"\b\d{8,10}:[A-Za-z0-9_-]{30,}\b", script);
        Assert.DoesNotMatch(@"(?im)^\s*(?:&\s*)?docker\b", script);
    }

    [Fact]
    public void ScriptHandlesDetachedHeadWithoutRequiringSpecificBranchName()
    {
        var script = File.ReadAllText(ScriptPath);
        AssertContainsAll(script,
            "function Invoke-GitText",
            "GITHUB_HEAD_REF",
            "GITHUB_REF_NAME",
            "$branch = \"detached\"",
            "$head = Invoke-GitText");
        Assert.DoesNotContain("(& git branch --show-current).Trim()", script, StringComparison.Ordinal);
        Assert.DoesNotContain("(& git rev-parse HEAD).Trim()", script, StringComparison.Ordinal);
    }

    [Fact]
    public void DryRunDocContainsRequiredSafetyBoundary()
    {
        var doc = File.ReadAllText(DocPath);
        AssertContainsAll(doc,
            "closed beta only",
            "not production or public release",
            "no real secrets in Git",
            "no real domains",
            "no real chat IDs",
            "Telegram disabled by default",
            "chat ID discovery disabled by default",
            "No Telegram network calls",
            "no setWebhook execution",
            "no long polling",
            "No DB/audit persistence",
            "external monitoring",
            "Runtime catalog is only final-answer source",
            "Manual-codebook/staging/preview are not final diagnosis",
            "Vendor manual coverage remains partial");
    }

    [Fact]
    public void DryRunDocAndScriptContainNoForbiddenClaimsOrRealDomains()
    {
        var combined = string.Join(Environment.NewLine, File.ReadAllText(DocPath), File.ReadAllText(ScriptPath));
        foreach (var claim in ForbiddenClaims)
        {
            Assert.DoesNotContain(claim, combined, StringComparison.OrdinalIgnoreCase);
        }

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
    public void FocusedDryRunCreatesIgnoredReportsAndExitsZero()
    {
        var outputRoot = Path.Combine(
            "artifacts",
            "verification",
            "equipment-diagnostics",
            "telegram-deployment-dry-run-test",
            Guid.NewGuid().ToString("N"));
        var fullOutputRoot = Path.Combine(TestPaths.RepoRoot, outputRoot);

        try
        {
            var result = RunPowerShell(
                ".\\scripts\\equipment-diagnostics\\prepare-telegram-closed-beta-deployment-dry-run.ps1 " +
                $"-BaseRef origin/master -OutputRoot \"{outputRoot}\" " +
                "-SkipDockerComposeConfig -SkipDeploymentScaffoldValidation " +
                "-SkipProductionEnvValidation -SkipReleaseEvidenceReference");

            Assert.True(result.ExitCode == 0, result.Output);
            Assert.Contains("Status: PASS", result.Output, StringComparison.OrdinalIgnoreCase);
            Assert.True(File.Exists(Path.Combine(fullOutputRoot, "deployment-dry-run-summary.md")));
            Assert.True(File.Exists(Path.Combine(fullOutputRoot, "deployment-dry-run-report.json")));
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
        string.Concat("public release ", "ready"),
        string.Concat("fully autonomous ", "engineer"),
        string.Concat("autonomous production ", "execution"),
        string.Concat("AI ", "diagnosis"),
        string.Concat("RAG ", "diagnosis"),
        string.Concat("vector search ", "diagnosis"),
        string.Concat("full vendor manual ", "coverage"),
        string.Concat("full ", "parity"),
        string.Concat("ManualVerified ", "promotion")
    ];

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
