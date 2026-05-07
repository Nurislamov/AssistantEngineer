using AssistantEngineer.Tests;

namespace AssistantEngineer.Tests.Validation.EnergyPlus;

public class EnergyPlusValidationCSharpToolArchitectureTests
{
    [Fact]
    public void EnergyPlusValidationToolProjectReadmeAndArchitectureDocExist()
    {
        Assert.True(File.Exists(ToolProjectPath), $"Tool project is missing: {ToolProjectPath}");
        Assert.True(File.Exists(ToolProgramPath), $"Tool program is missing: {ToolProgramPath}");
        Assert.True(File.Exists(ToolRunnerPath), $"Tool runner is missing: {ToolRunnerPath}");
        Assert.True(File.Exists(ToolReadmePath), $"Tool README is missing: {ToolReadmePath}");
        Assert.True(File.Exists(ToolArchitectureDocPath), $"Tool architecture doc is missing: {ToolArchitectureDocPath}");
    }

    [Fact]
    public void EnergyPlusValidationToolOwnsValidationCommands()
    {
        var content = ReadToolSourceBundle();

        var requiredPhrases = new[]
        {
            "compare-fixtures",
            "assert-smoke001-real-fixture-ready",
            "generate-fixture-catalog",
            "generate-comparison-summary",
            "generate-validation-readiness",
            "generate-validation-evidence",
            "regenerate-validation-artifacts",
            "verify-validation",
            "EnergyPlusValidationCaseRegistry.json",
            "GenericEnergyPlusValidationFixtureRunner",
            "RealEnergyPlusComparison",
            "PlaceholderComparison"
        };

        foreach (var requiredPhrase in requiredPhrases)
        {
            Assert.Contains(requiredPhrase, content, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void EnergyPlusValidationProgram_RemainsThinCompositionRoot()
    {
        var programLines = File.ReadAllLines(ToolProgramPath);
        Assert.True(programLines.Length <= 180, $"Program.cs should stay thin (actual lines: {programLines.Length}).");

        var program = File.ReadAllText(ToolProgramPath);
        Assert.Contains("EnergyPlusValidationToolRunner", program, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("compare-energyplus-validation-fixtures.ps1", "compare-fixtures")]
    [InlineData("assert-ep-smoke-001-real-fixture-ready.ps1", "assert-smoke001-real-fixture-ready")]
    [InlineData("generate-energyplus-validation-fixture-catalog.ps1", "generate-fixture-catalog")]
    [InlineData("generate-engineering-core-v1-validation-comparison-summary.ps1", "generate-comparison-summary")]
    [InlineData("generate-engineering-core-v1-validation-readiness.ps1", "generate-validation-readiness")]
    [InlineData("generate-ep-smoke-001-comparison-readiness.ps1", "generate-smoke001-comparison-readiness")]
    [InlineData("generate-engineering-core-v1-validation-evidence.ps1", "generate-validation-evidence")]
    [InlineData("regenerate-engineering-core-v1-validation-artifacts.ps1", "regenerate-validation-artifacts")]
    [InlineData("verify-engineering-core-v1-validation.ps1", "verify-validation")]
    [InlineData("compare-ep-smoke-001-placeholder.ps1", "compare-fixtures")]
    public void EnergyPlusValidationScriptsAreThinWrappers(string scriptName, string command)
    {
        var scriptPath = Path.Combine(TestPaths.RepoRoot, "scripts", "engineering-core", scriptName);

        Assert.True(File.Exists(scriptPath), $"Expected wrapper script to exist: {scriptPath}");

        var content = File.ReadAllText(scriptPath);

        Assert.Contains("AssistantEngineer.Tools.EnergyPlusValidation.csproj", content, StringComparison.Ordinal);
        Assert.Contains(command, content, StringComparison.Ordinal);
        Assert.Contains("dotnet run --project", content, StringComparison.Ordinal);

        Assert.DoesNotContain("ConvertTo-Json", content, StringComparison.Ordinal);
        Assert.DoesNotContain("Get-ChildItem -Path", content, StringComparison.Ordinal);
        Assert.DoesNotContain("Import-Csv", content, StringComparison.Ordinal);
    }

    [Fact]
    public void EnergyPlusValidationToolArchitectureDocStatesBoundariesAndNonClaims()
    {
        var content = File.ReadAllText(ToolArchitectureDocPath);

        var requiredPhrases = new[]
        {
            "EnergyPlus Validation Tool Architecture",
            "C# tools",
            "thin wrappers only",
            "docs/reports/validation",
            "tests/fixtures/validation/energyplus",
            "does not claim exact EnergyPlus numerical parity",
            "does not claim ASHRAE 140 validation coverage"
        };

        foreach (var requiredPhrase in requiredPhrases)
        {
            Assert.Contains(requiredPhrase, content, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static string ToolProjectPath =>
        Path.Combine(TestPaths.RepoRoot, "tools", "AssistantEngineer.Tools.EnergyPlusValidation", "AssistantEngineer.Tools.EnergyPlusValidation.csproj");

    private static string ToolProgramPath =>
        Path.Combine(TestPaths.RepoRoot, "tools", "AssistantEngineer.Tools.EnergyPlusValidation", "Program.cs");

    private static string ToolRunnerPath =>
        Path.Combine(TestPaths.RepoRoot, "tools", "AssistantEngineer.Tools.EnergyPlusValidation", "EnergyPlusValidationToolRunner.cs");

    private static string ToolReadmePath =>
        Path.Combine(TestPaths.RepoRoot, "tools", "AssistantEngineer.Tools.EnergyPlusValidation", "README.md");

    private static string ToolArchitectureDocPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "validation", "EnergyPlusValidationToolArchitecture.md");

    private static string ReadToolSourceBundle() =>
        string.Join(
            Environment.NewLine,
            File.ReadAllText(ToolProgramPath),
            File.ReadAllText(ToolRunnerPath));
}
