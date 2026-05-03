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
    public void ToolOwnsScriptBoundaryAuditCommand()
    {
        var content = File.ReadAllText(ToolProgramPath);

        var requiredPhrases = new[]
        {
            "audit-script-boundaries",
            "--fail-on-heavy-scripts",
            "ThinWrapper",
            "HeavyPowerShellLogic",
            "Repository Script Boundary Audit",
            "scripts",
            "tools"
        };

        foreach (var requiredPhrase in requiredPhrases)
        {
            Assert.Contains(requiredPhrase, content, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void AuditJsonDeclaresRepositoryBoundaryAndScriptTotals()
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
    }

    [Fact]
    public void AuditMarkdownDocumentsBoundaryAndMigrationStatus()
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
            "Heavy PowerShell scripts"
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
