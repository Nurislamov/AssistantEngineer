namespace AssistantEngineer.Tests.Operations;

public sealed class OperationalIncidentRunbookTests
{
    private static readonly string IncidentRoot = Path.Combine(
        TestPaths.RepoRoot, "docs", "operations", "incidents");

    [Fact]
    public void RequiredIncidentRunbooksExistAndStateSafetyBoundaries()
    {
        var required = new[]
        {
            "api-health-readiness-runbook.md",
            "telegram-webhook-runbook.md",
            "deployment-smoke-failure-runbook.md",
            "correlation-id-troubleshooting.md",
            "incident-report-template.md"
        };

        Assert.All(required, file => Assert.True(File.Exists(Path.Combine(IncidentRoot, file)), file));
        var docs = string.Join(Environment.NewLine, required.Select(file =>
            File.ReadAllText(Path.Combine(IncidentRoot, file))));

        Assert.Contains("X-Correlation-ID", docs, StringComparison.Ordinal);
        Assert.Contains("no external monitoring", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("audit persistence", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Telegram message bod", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("chat IDs", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secret", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("sanitized", docs, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void OperationsIndexReferencesRunbooksAndSanitizedCollection()
    {
        var index = File.ReadAllText(Path.Combine(TestPaths.RepoRoot, "docs", "operations", "README.md"));

        Assert.Contains("incident runbooks", index, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("collect-sanitized-logs.ps1", index, StringComparison.Ordinal);
        Assert.Contains("artifacts/operations/", index, StringComparison.Ordinal);
    }

    [Fact]
    public void IncidentArtifactsAndLogDumpsAreIgnoredAndNotTracked()
    {
        var ignore = File.ReadAllText(Path.Combine(TestPaths.RepoRoot, ".gitignore"));

        Assert.Contains("/artifacts/operations/", ignore, StringComparison.Ordinal);
        Assert.Contains("*.log", ignore, StringComparison.Ordinal);
        Assert.Empty(RunGit("ls-files", "artifacts/operations/*"));
        Assert.Empty(RunGit("ls-files", "*.log"));
        Assert.Empty(RunGit("ls-files", "*.pdf"));
    }

    private static string[] RunGit(params string[] arguments)
    {
        var start = new System.Diagnostics.ProcessStartInfo("git")
        {
            WorkingDirectory = TestPaths.RepoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        foreach (var argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }

        using var process = System.Diagnostics.Process.Start(start);
        Assert.NotNull(process);
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        Assert.Equal(0, process.ExitCode);
        return output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
    }
}
