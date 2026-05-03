using AssistantEngineer.Tests;

namespace AssistantEngineer.Tests.Tools;

public class EngineeringCoreContractsToolArchitectureTests
{
    [Fact]
    public void EngineeringCoreContractsToolProjectReadmeAndArchitectureDocExist()
    {
        var requiredFiles = new[]
        {
            ToolProjectPath,
            ToolProgramPath,
            ToolReadmePath,
            ArchitectureDocPath,
            ApiSnapshotWrapperPath,
            ReportSnapshotWrapperPath
        };

        foreach (var requiredFile in requiredFiles)
        {
            Assert.True(File.Exists(requiredFile), $"Required Engineering Core contracts artifact is missing: {requiredFile}");
        }
    }

    [Fact]
    public void EngineeringCoreContractsToolOwnsSnapshotGenerationCommands()
    {
        var content = File.ReadAllText(ToolProgramPath);

        var requiredPhrases = new[]
        {
            "generate-api-contract-snapshots",
            "generate-report-contract-snapshots",
            "generate-all-contract-snapshots",
            "EngineeringCoreV1Manifest.json",
            "EngineeringCoreV1DiagnosticsCatalog.json",
            "status.sample.json",
            "diagnostics-catalog.sample.json",
            "heating-report.sample.json",
            "cooling-report.sample.json",
            "annual-energy-disclosure.sample.json"
        };

        foreach (var requiredPhrase in requiredPhrases)
        {
            Assert.Contains(requiredPhrase, content, StringComparison.Ordinal);
        }
    }

    [Theory]
    [InlineData("generate-engineering-core-v1-api-contract-snapshots.ps1", "generate-api-contract-snapshots")]
    [InlineData("generate-engineering-core-v1-report-contract-snapshots.ps1", "generate-report-contract-snapshots")]
    public void ContractSnapshotScriptsAreThinWrappers(string scriptName, string command)
    {
        var scriptPath = Path.Combine(TestPaths.RepoRoot, "scripts", "engineering-core", scriptName);

        Assert.True(File.Exists(scriptPath), $"Expected wrapper script to exist: {scriptPath}");

        var content = File.ReadAllText(scriptPath);

        Assert.Contains("AssistantEngineer.Tools.EngineeringCoreContracts.csproj", content, StringComparison.Ordinal);
        Assert.Contains(command, content, StringComparison.Ordinal);
        Assert.Contains("dotnet run --project", content, StringComparison.Ordinal);

        Assert.DoesNotContain("ConvertTo-Json", content, StringComparison.Ordinal);
        Assert.DoesNotContain("ConvertFrom-Json", content, StringComparison.Ordinal);
        Assert.DoesNotContain("[ordered]@", content, StringComparison.Ordinal);
        Assert.DoesNotContain("New-Item -ItemType Directory", content, StringComparison.Ordinal);
    }

    [Fact]
    public void ArchitectureDocDocumentsContractsToolBoundaryAndNonClaims()
    {
        var content = File.ReadAllText(ArchitectureDocPath);

        var requiredPhrases = new[]
        {
            "Engineering Core Contracts Tool Architecture",
            "C# tools",
            "thin wrappers",
            "JSON construction",
            "manifest parsing",
            "report sample construction",
            "does not claim exact EnergyPlus numerical parity",
            "does not claim ASHRAE 140 validation coverage"
        };

        foreach (var requiredPhrase in requiredPhrases)
        {
            Assert.Contains(requiredPhrase, content, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static string ToolProjectPath =>
        Path.Combine(TestPaths.RepoRoot, "tools", "AssistantEngineer.Tools.EngineeringCoreContracts", "AssistantEngineer.Tools.EngineeringCoreContracts.csproj");

    private static string ToolProgramPath =>
        Path.Combine(TestPaths.RepoRoot, "tools", "AssistantEngineer.Tools.EngineeringCoreContracts", "Program.cs");

    private static string ToolReadmePath =>
        Path.Combine(TestPaths.RepoRoot, "tools", "AssistantEngineer.Tools.EngineeringCoreContracts", "README.md");

    private static string ArchitectureDocPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "engineering-core", "EngineeringCoreContractsToolArchitecture.md");

    private static string ApiSnapshotWrapperPath =>
        Path.Combine(TestPaths.RepoRoot, "scripts", "engineering-core", "generate-engineering-core-v1-api-contract-snapshots.ps1");

    private static string ReportSnapshotWrapperPath =>
        Path.Combine(TestPaths.RepoRoot, "scripts", "engineering-core", "generate-engineering-core-v1-report-contract-snapshots.ps1");
}
