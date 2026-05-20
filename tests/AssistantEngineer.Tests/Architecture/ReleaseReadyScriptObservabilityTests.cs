using System.Text.Json;
using AssistantEngineer.Tests;

namespace AssistantEngineer.Tests.Architecture;

public sealed class ReleaseReadyScriptObservabilityTests
{
    [Fact]
    public void ReleaseReadyWrapperAndToolContainObservabilityDiagnostics()
    {
        var wrapper = File.ReadAllText(ReleaseReadyWrapperPath);
        var tool = File.ReadAllText(ReleaseToolProgramPath);

        Assert.Contains("assert-release-ready", wrapper, StringComparison.Ordinal);
        Assert.Contains("--quiet-stages", wrapper, StringComparison.Ordinal);
        Assert.Contains("--output-summary-json", wrapper, StringComparison.Ordinal);

        Assert.Contains("Stage start (UTC)", tool, StringComparison.Ordinal);
        Assert.Contains("Stage end (UTC)", tool, StringComparison.Ordinal);
        Assert.Contains("Deterministic stage summary:", tool, StringComparison.Ordinal);
        Assert.Contains("Release-ready gate failed.", tool, StringComparison.Ordinal);
        Assert.Contains("Failed stage:", tool, StringComparison.Ordinal);
        Assert.Contains("TryWriteSummaryJson", tool, StringComparison.Ordinal);
    }

    [Fact]
    public void DefaultPathDoesNotSkipChecksOrHideFailures()
    {
        var tool = File.ReadAllText(ReleaseToolProgramPath);

        Assert.DoesNotContain("catch { return 0; }", tool, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("WriteWarning(\"FAILED", tool, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Restore solution", tool, StringComparison.Ordinal);
        Assert.Contains("Build solution (Debug)", tool, StringComparison.Ordinal);
        Assert.Contains("Smoke verification profile", tool, StringComparison.Ordinal);
        Assert.Contains("Contracts verification profile", tool, StringComparison.Ordinal);
        Assert.Contains("Manifest verification profile", tool, StringComparison.Ordinal);
        Assert.Contains("Full Engineering Core V1 verification", tool, StringComparison.Ordinal);
    }

    [Fact]
    public void ToolContainsNoSecretOrConnectionStringLoggingPatterns()
    {
        var tool = File.ReadAllText(ReleaseToolProgramPath);
        Assert.DoesNotContain("Console.WriteLine(connectionString", tool, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Console.Error.WriteLine(connectionString", tool, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Password=super-secret", tool, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void OutputSummaryJsonOptionHasIgnoredArtifactsRoot()
    {
        var wrapper = File.ReadAllText(ReleaseReadyWrapperPath);
        Assert.Contains("--output-summary-json", wrapper, StringComparison.Ordinal);

        var gitignore = File.ReadAllText(Path.Combine(TestPaths.RepoRoot, ".gitignore"));
        Assert.Contains("artifacts/", gitignore, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ReleaseReadyObservabilityAuditSchemaParses()
    {
        var schemaPath = Path.Combine(TestPaths.RepoRoot, "docs", "security", "release-ready-observability-audit.schema.json");
        Assert.True(File.Exists(schemaPath));
        using var schema = JsonDocument.Parse(File.ReadAllText(schemaPath));
        Assert.True(schema.RootElement.TryGetProperty("requiredFields", out _));
    }

    private static string ReleaseReadyWrapperPath =>
        Path.Combine(TestPaths.RepoRoot, "scripts", "engineering-core", "assert-engineering-core-v1-release-ready.ps1");

    private static string ReleaseToolProgramPath =>
        Path.Combine(TestPaths.RepoRoot, "tools", "AssistantEngineer.Tools.EngineeringCoreRelease", "Program.cs");
}

