using System.Diagnostics;
using System.Text.Json;

namespace AssistantEngineer.Tests.Operations;

public sealed class OperationalLogRedactionScriptTests
{
    private static readonly string ScriptsRoot = Path.Combine(
        TestPaths.RepoRoot, "scripts", "operations");

    [Fact]
    public void OperationsScriptsExistAreInventoriedAndUseSafeBoundaries()
    {
        var redactor = File.ReadAllText(Path.Combine(ScriptsRoot, "redact-log-file.ps1"));
        var collector = File.ReadAllText(Path.Combine(ScriptsRoot, "collect-sanitized-logs.ps1"));
        using var inventory = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            TestPaths.RepoRoot, "docs", "architecture", "scripts-tools-inventory.json")));
        var paths = inventory.RootElement.GetProperty("entries").EnumerateArray()
            .Select(entry => entry.GetProperty("path").GetString())
            .ToArray();

        Assert.Contains("scripts/operations/redact-log-file.ps1", paths);
        Assert.Contains("scripts/operations/collect-sanitized-logs.ps1", paths);
        Assert.Contains("RedactOnlyInputPath", collector, StringComparison.Ordinal);
        Assert.Contains("artifacts/operations/sanitized-logs", collector, StringComparison.Ordinal);
        Assert.Contains("InputContent", redactor, StringComparison.Ordinal);
        Assert.DoesNotContain("Write-Host $InputContent", redactor, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Set-Content", collector, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Write-Host $rawLogs", collector, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("No log source selected", collector, StringComparison.Ordinal);
    }

    [Fact]
    public void RedactOnlyModeWritesOnlySanitizedOutput()
    {
        var temp = Path.Combine(Path.GetTempPath(), $"assistant-engineer-redaction-{Guid.NewGuid():N}");
        Directory.CreateDirectory(temp);
        try
        {
            var input = Path.Combine(temp, "input.txt");
            var output = Path.Combine(temp, "output.txt");
            var token = $"123456789:{new string('A', 35)}";
            File.WriteAllText(input, string.Join(Environment.NewLine,
                $"Authorization: Bearer private-value",
                "X-Telegram-Bot-Api-Secret-Token: webhook-secret",
                "AllowedChatIds=123,456",
                "DeniedChatIds: 789",
                "{\"chat_id\":123456,\"text\":\"raw Telegram message\",\"message_body\":\"raw body\"}",
                token));

            var result = RunPowerShell(
                Path.Combine(ScriptsRoot, "collect-sanitized-logs.ps1"),
                "-RedactOnlyInputPath", input,
                "-OutputPath", output);
            var sanitized = File.ReadAllText(output);

            Assert.Equal(0, result.ExitCode);
            Assert.Contains("[REDACTED]", sanitized, StringComparison.Ordinal);
            Assert.DoesNotContain("private-value", sanitized, StringComparison.Ordinal);
            Assert.DoesNotContain("webhook-secret", sanitized, StringComparison.Ordinal);
            Assert.DoesNotContain("123,456", sanitized, StringComparison.Ordinal);
            Assert.DoesNotContain("\"chat_id\":123456", sanitized, StringComparison.Ordinal);
            Assert.DoesNotContain("raw Telegram message", sanitized, StringComparison.Ordinal);
            Assert.DoesNotContain("raw body", sanitized, StringComparison.Ordinal);
            Assert.DoesNotContain(token, sanitized, StringComparison.Ordinal);
            Assert.DoesNotContain(token, result.Output, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(temp, recursive: true);
        }
    }

    private static (int ExitCode, string Output) RunPowerShell(string script, params string[] arguments)
    {
        var start = new ProcessStartInfo("powershell")
        {
            WorkingDirectory = TestPaths.RepoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        start.ArgumentList.Add("-NoProfile");
        start.ArgumentList.Add("-File");
        start.ArgumentList.Add(script);
        foreach (var argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }

        using var process = Process.Start(start);
        Assert.NotNull(process);
        var output = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
        process.WaitForExit();
        return (process.ExitCode, output);
    }
}
