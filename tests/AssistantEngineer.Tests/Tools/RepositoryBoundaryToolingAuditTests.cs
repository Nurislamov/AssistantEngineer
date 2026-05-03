using System.Text.Json;
using AssistantEngineer.Tests;

namespace AssistantEngineer.Tests.Tools;

public class RepositoryBoundaryToolingAuditTests
{
    [Fact]
    public void RepositoryBoundaryToolProjectReadmePolicyAndReportExist()
    {
        var requiredFiles = new[]
        {
            ToolProjectPath,
            ToolProgramPath,
            ToolReadmePath,
            PolicyPath,
            AuditJsonPath,
            AuditMarkdownPath
        };

        foreach (var requiredFile in requiredFiles)
        {
            Assert.True(File.Exists(requiredFile), $"Required repository boundary artifact is missing: {requiredFile}");
        }
    }

    [Fact]
    public void ToolOwnsScriptBoundaryAuditCommandAndStrictMode()
    {
        var content = File.ReadAllText(ToolProgramPath);

        var requiredPhrases = new[]
        {
            "audit-script-boundaries",
            "--strict",
            "--fail-on-heavy-scripts",
            "--fail-on-unknown-scripts",
            "ThinWrapper",
            "HeavyPowerShellLogic",
            "UnknownPowerShellScript",
            "Repository Script Boundary Audit",
            "strictModeReady",
            "nonThinScripts"
        };

        foreach (var requiredPhrase in requiredPhrases)
        {
            Assert.Contains(requiredPhrase, content, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void AuditJsonDeclaresRepositoryBoundaryAndStrictScriptTotals()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(AuditJsonPath));
        var root = document.RootElement;

        Assert.Equal("Repository Script Boundary Audit", root.GetProperty("reportName").GetString());
        Assert.Equal("v1", root.GetProperty("version").GetString());
        Assert.Equal("2026-01-01 00:00:00 UTC", root.GetProperty("generatedAtUtc").GetString());

        var policy = root.GetProperty("policy");
        Assert.Equal("application code", policy.GetProperty("srcBackend").GetString());
        Assert.Equal("frontend code", policy.GetProperty("srcFrontend").GetString());
        Assert.Equal("test code", policy.GetProperty("tests").GetString());
        Assert.Equal("documentation and generated evidence", policy.GetProperty("docs").GetString());
        Assert.Equal("C# automation, validation and release tools", policy.GetProperty("tools").GetString());
        Assert.Equal("thin wrappers only", policy.GetProperty("scripts").GetString());

        var totals = root.GetProperty("totals");

        Assert.True(totals.GetProperty("scripts").GetInt32() >= 1);
        Assert.True(totals.GetProperty("thinScripts").GetInt32() >= 1);
        Assert.Equal(0, totals.GetProperty("heavyScripts").GetInt32());
        Assert.Equal(0, totals.GetProperty("unknownScripts").GetInt32());
        Assert.Equal(0, totals.GetProperty("nonThinScripts").GetInt32());

        Assert.True(root.GetProperty("strictModeReady").GetBoolean());
        Assert.Equal("Compliant", root.GetProperty("status").GetString());
    }

    [Fact]
    public void AllScriptsAreClassifiedAsThinWrappersAndHaveKnownTargets()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(AuditJsonPath));
        var scripts = document.RootElement.GetProperty("scripts").EnumerateArray().ToArray();

        Assert.NotEmpty(scripts);

        foreach (var script in scripts)
        {
            Assert.Equal("ThinWrapper", script.GetProperty("classification").GetString());

            var targetTool = script.GetProperty("targetTool").GetString();

            Assert.False(
                string.IsNullOrWhiteSpace(targetTool),
                $"Script has no target tool: {script.GetProperty("path").GetString()}");
        }
    }

    [Fact]
    public void AuditMarkdownDocumentsStrictBoundaryAndMigrationStatus()
    {
        var content = File.ReadAllText(AuditMarkdownPath);

        var requiredPhrases = new[]
        {
            "Repository Script Boundary Audit",
            "src/Backend",
            "src/Frontend",
            "tests",
            "docs",
            "tools",
            "scripts",
            "thin wrappers",
            "Heavy PowerShell scripts",
            "Unknown PowerShell scripts",
            "Non-thin PowerShell scripts",
            "Strict mode ready",
            "All audited PowerShell scripts are known thin wrappers"
        };

        foreach (var requiredPhrase in requiredPhrases)
        {
            Assert.Contains(requiredPhrase, content, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void PolicyDocumentsThinWrapperDefinitionAndStrictMode()
    {
        var content = File.ReadAllText(PolicyPath);

        var requiredPhrases = new[]
        {
            "Thin wrapper definition",
            "accept PowerShell-friendly parameters",
            "call `dotnet run --project",
            "must not own generation, validation or release logic",
            "--strict",
            "UnknownPowerShellScript",
            "HeavyPowerShellLogic"
        };

        foreach (var requiredPhrase in requiredPhrases)
        {
            Assert.Contains(requiredPhrase, content, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static string ToolProjectPath =>
        Path.Combine(TestPaths.RepoRoot, "tools", "AssistantEngineer.Tools.RepositoryBoundaries", "AssistantEngineer.Tools.RepositoryBoundaries.csproj");

    private static string ToolProgramPath =>
        Path.Combine(TestPaths.RepoRoot, "tools", "AssistantEngineer.Tools.RepositoryBoundaries", "Program.cs");

    private static string ToolReadmePath =>
        Path.Combine(TestPaths.RepoRoot, "tools", "AssistantEngineer.Tools.RepositoryBoundaries", "README.md");

    private static string PolicyPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "engineering-core", "RepositoryBoundaryToolingPolicy.md");

    private static string AuditJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "reports", "repository", "ScriptBoundaryAudit.json");

    private static string AuditMarkdownPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "reports", "repository", "ScriptBoundaryAudit.md");
}
