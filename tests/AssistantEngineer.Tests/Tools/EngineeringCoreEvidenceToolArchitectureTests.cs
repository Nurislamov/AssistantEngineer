using AssistantEngineer.Tests;

namespace AssistantEngineer.Tests.Tools;

public class EngineeringCoreEvidenceToolArchitectureTests
{
    [Fact]
    public void EngineeringCoreEvidenceToolProjectReadmeAndArchitectureDocExist()
    {
        var requiredFiles = new[]
        {
            ToolProjectPath,
            ToolProgramPath,
            ToolRunnerPath,
            ToolReadmePath,
            ArchitectureDocPath,
            ReleaseEvidenceWrapperPath,
            ExportChecklistWrapperPath,
            TraceabilityWrapperPath
        };

        foreach (var requiredFile in requiredFiles)
        {
            Assert.True(File.Exists(requiredFile), $"Required Engineering Core evidence artifact is missing: {requiredFile}");
        }
    }

    [Fact]
    public void EngineeringCoreEvidenceToolOwnsEvidenceGenerationCommands()
    {
        var content = ReadToolSourceBundle();

        var requiredPhrases = new[]
        {
            "generate-release-evidence",
            "generate-export-disclosure-checklist",
            "generate-traceability-matrix",
            "generate-all-evidence",
            "Engineering Core V1 Release Evidence",
            "Engineering Core V1 Export Disclosure Checklist",
            "Engineering Core V1 Traceability Matrix",
            "EngineeringCoreV1Manifest.json",
            "EngineeringCoreV1DiagnosticsCatalog.json",
            "EnergyPlusValidationCaseRegistry.json"
        };

        foreach (var requiredPhrase in requiredPhrases)
        {
            Assert.Contains(requiredPhrase, content, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void EngineeringCoreEvidenceProgram_RemainsThinCompositionRoot()
    {
        var programLines = File.ReadAllLines(ToolProgramPath);
        Assert.True(programLines.Length <= 140, $"Program.cs should stay thin (actual lines: {programLines.Length}).");

        var program = File.ReadAllText(ToolProgramPath);
        Assert.Contains("EngineeringCoreEvidenceToolRunner", program, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("generate-engineering-core-v1-release-evidence.ps1", "generate-release-evidence")]
    [InlineData("generate-engineering-core-v1-export-disclosure-checklist.ps1", "generate-export-disclosure-checklist")]
    [InlineData("generate-engineering-core-v1-traceability-matrix.ps1", "generate-traceability-matrix")]
    public void EvidenceScriptsAreThinWrappers(string scriptName, string command)
    {
        var scriptPath = Path.Combine(TestPaths.RepoRoot, "scripts", "engineering-core", scriptName);

        Assert.True(File.Exists(scriptPath), $"Expected wrapper script to exist: {scriptPath}");

        var content = File.ReadAllText(scriptPath);

        Assert.Contains("AssistantEngineer.Tools.EngineeringCoreEvidence.csproj", content, StringComparison.Ordinal);
        Assert.Contains(command, content, StringComparison.Ordinal);
        Assert.Contains("dotnet run --project", content, StringComparison.Ordinal);

        Assert.DoesNotContain("ConvertFrom-Json", content, StringComparison.Ordinal);
        Assert.DoesNotContain("ConvertTo-Json", content, StringComparison.Ordinal);
        Assert.DoesNotContain("ForEach-Object", content, StringComparison.Ordinal);
        Assert.DoesNotContain("[ordered]@", content, StringComparison.Ordinal);
    }

    [Fact]
    public void ArchitectureDocDocumentsEvidenceToolBoundaryAndNonClaims()
    {
        var content = File.ReadAllText(ArchitectureDocPath);

        var requiredPhrases = new[]
        {
            "Engineering Core Evidence Tool Architecture",
            "C# tools",
            "thin wrappers",
            "Markdown generation",
            "JSON parsing",
            "traceability matrix logic",
            "export checklist logic",
            "does not claim exact EnergyPlus numerical parity",
            "does not claim ASHRAE 140 validation coverage"
        };

        foreach (var requiredPhrase in requiredPhrases)
        {
            Assert.Contains(requiredPhrase, content, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static string ToolProjectPath =>
        Path.Combine(TestPaths.RepoRoot, "tools", "AssistantEngineer.Tools.EngineeringCoreEvidence", "AssistantEngineer.Tools.EngineeringCoreEvidence.csproj");

    private static string ToolProgramPath =>
        Path.Combine(TestPaths.RepoRoot, "tools", "AssistantEngineer.Tools.EngineeringCoreEvidence", "Program.cs");

    private static string ToolRunnerPath =>
        Path.Combine(TestPaths.RepoRoot, "tools", "AssistantEngineer.Tools.EngineeringCoreEvidence", "EngineeringCoreEvidenceToolRunner.cs");

    private static string ToolReadmePath =>
        Path.Combine(TestPaths.RepoRoot, "tools", "AssistantEngineer.Tools.EngineeringCoreEvidence", "README.md");

    private static string ArchitectureDocPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "engineering-core", "EngineeringCoreEvidenceToolArchitecture.md");

    private static string ReleaseEvidenceWrapperPath =>
        Path.Combine(TestPaths.RepoRoot, "scripts", "engineering-core", "generate-engineering-core-v1-release-evidence.ps1");

    private static string ExportChecklistWrapperPath =>
        Path.Combine(TestPaths.RepoRoot, "scripts", "engineering-core", "generate-engineering-core-v1-export-disclosure-checklist.ps1");

    private static string TraceabilityWrapperPath =>
        Path.Combine(TestPaths.RepoRoot, "scripts", "engineering-core", "generate-engineering-core-v1-traceability-matrix.ps1");

    private static string ReadToolSourceBundle() =>
        string.Join(
            Environment.NewLine,
            File.ReadAllText(ToolProgramPath),
            File.ReadAllText(ToolRunnerPath));
}
