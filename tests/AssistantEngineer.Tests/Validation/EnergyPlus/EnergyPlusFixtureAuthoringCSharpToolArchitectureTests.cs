using AssistantEngineer.Tests;

namespace AssistantEngineer.Tests.Validation.EnergyPlus;

public class EnergyPlusFixtureAuthoringCSharpToolArchitectureTests
{
    [Fact]
    public void FixtureAuthoringToolProjectReadmeAndArchitectureDocExist()
    {
        var requiredFiles = new[]
        {
            ToolProjectPath,
            ToolProgramPath,
            ToolReadmePath,
            ArchitectureDocPath,
            WrapperScriptPath
        };

        foreach (var requiredFile in requiredFiles)
        {
            Assert.True(File.Exists(requiredFile), $"Required fixture authoring tool artifact is missing: {requiredFile}");
        }
    }

    [Fact]
    public void FixtureAuthoringToolOwnsScaffoldGeneration()
    {
        var content = File.ReadAllText(ToolProgramPath);

        var requiredPhrases = new[]
        {
            "new-fixture",
            "CaseId must use uppercase letters",
            "case-metadata.template.json",
            "assistantengineer-input.template.json",
            "reference-output.placeholder.template.json",
            "comparison-tolerances.template.json",
            "README.template.md",
            "EnergyPlusValidationCaseRegistry.json",
            "compare-energyplus-validation-fixtures.ps1",
            "generate-energyplus-validation-fixture-catalog.ps1"
        };

        foreach (var requiredPhrase in requiredPhrases)
        {
            Assert.Contains(requiredPhrase, content, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void NewFixtureScriptIsThinWrapper()
    {
        var content = File.ReadAllText(WrapperScriptPath);

        Assert.Contains("AssistantEngineer.Tools.EnergyPlusFixtureAuthoring.csproj", content, StringComparison.Ordinal);
        Assert.Contains("new-fixture", content, StringComparison.Ordinal);
        Assert.Contains("dotnet run --project", content, StringComparison.Ordinal);
        Assert.Contains("CaseId", content, StringComparison.Ordinal);
        Assert.Contains("Name", content, StringComparison.Ordinal);
        Assert.Contains("Stage", content, StringComparison.Ordinal);
        Assert.Contains("Force", content, StringComparison.Ordinal);

        Assert.DoesNotContain("function Expand-Template", content, StringComparison.Ordinal);
        Assert.DoesNotContain("Set-Content $DestinationPath", content, StringComparison.Ordinal);
        Assert.DoesNotContain("Get-Content $TemplatePath", content, StringComparison.Ordinal);
        Assert.DoesNotContain("$tokens = @", content, StringComparison.Ordinal);
    }

    [Fact]
    public void ArchitectureDocDocumentsFixtureAuthoringBoundaryAndNonClaims()
    {
        var content = File.ReadAllText(ArchitectureDocPath);

        var requiredPhrases = new[]
        {
            "EnergyPlus Fixture Authoring Tool Architecture",
            "C# tool",
            "thin wrappers",
            "CaseId validation",
            "template expansion",
            "fixture directory creation",
            "PlaceholderComparison is not real EnergyPlus validation",
            "does not claim exact EnergyPlus numerical parity",
            "does not claim ASHRAE 140 validation coverage"
        };

        foreach (var requiredPhrase in requiredPhrases)
        {
            Assert.Contains(requiredPhrase, content, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static string ToolProjectPath =>
        Path.Combine(TestPaths.RepoRoot, "tools", "AssistantEngineer.Tools.EnergyPlusFixtureAuthoring", "AssistantEngineer.Tools.EnergyPlusFixtureAuthoring.csproj");

    private static string ToolProgramPath =>
        Path.Combine(TestPaths.RepoRoot, "tools", "AssistantEngineer.Tools.EnergyPlusFixtureAuthoring", "Program.cs");

    private static string ToolReadmePath =>
        Path.Combine(TestPaths.RepoRoot, "tools", "AssistantEngineer.Tools.EnergyPlusFixtureAuthoring", "README.md");

    private static string ArchitectureDocPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "validation", "EnergyPlusFixtureAuthoringToolArchitecture.md");

    private static string WrapperScriptPath =>
        Path.Combine(TestPaths.RepoRoot, "scripts", "engineering-core", "new-energyplus-validation-fixture.ps1");
}
