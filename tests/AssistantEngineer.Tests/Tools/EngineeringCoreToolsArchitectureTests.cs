using AssistantEngineer.Tests;

namespace AssistantEngineer.Tests.Tools;

public class EngineeringCoreToolsArchitectureTests
{
    [Fact]
    public void EngineeringCoreToolProjectReadmeAndArchitectureDocExist()
    {
        Assert.True(File.Exists(ToolProjectPath), $"Tool project is missing: {ToolProjectPath}");
        Assert.True(File.Exists(ToolProgramPath), $"Tool program is missing: {ToolProgramPath}");
        Assert.True(File.Exists(ToolReadmePath), $"Tool README is missing: {ToolReadmePath}");
        Assert.True(File.Exists(ToolsArchitectureDocPath), $"Tools architecture doc is missing: {ToolsArchitectureDocPath}");
    }

    [Fact]
    public void EngineeringCoreToolOwnsCalculationModuleCommands()
    {
        var content = File.ReadAllText(ToolProgramPath);

        var requiredPhrases = new[]
        {
            "generate-calculation-module-inventory",
            "verify-calculation-module-deepening",
            "verify-calculation-module-balance-invariants",
            "verify-calculation-module-diagnostics-consistency",
            "verify-calculation-module-deepening-all",
            "Calculation Module Deepening Inventory",
            "CalculationModuleDeepeningGuardTests",
            "CalculationModuleBalanceInvariantTests",
            "CalculationModuleDiagnosticsConsistencyTests"
        };

        foreach (var requiredPhrase in requiredPhrases)
        {
            Assert.Contains(requiredPhrase, content, StringComparison.Ordinal);
        }
    }

    [Theory]
    [InlineData("generate-calculation-module-inventory.ps1", "generate-calculation-module-inventory")]
    [InlineData("verify-calculation-module-deepening.ps1", "verify-calculation-module-deepening")]
    [InlineData("verify-calculation-module-balance-invariants.ps1", "verify-calculation-module-balance-invariants")]
    [InlineData("verify-calculation-module-diagnostics-consistency.ps1", "verify-calculation-module-diagnostics-consistency")]
    public void CalculationModuleScriptsAreThinWrappers(string scriptName, string command)
    {
        var scriptPath = Path.Combine(TestPaths.RepoRoot, "scripts", "engineering-core", scriptName);

        Assert.True(File.Exists(scriptPath), $"Expected wrapper script to exist: {scriptPath}");

        var content = File.ReadAllText(scriptPath);

        Assert.Contains("AssistantEngineer.Tools.EngineeringCore.csproj", content, StringComparison.Ordinal);
        Assert.Contains(command, content, StringComparison.Ordinal);
        Assert.Contains("dotnet run --project", content, StringComparison.Ordinal);

        Assert.DoesNotContain("Get-ChildItem -Path", content, StringComparison.Ordinal);
        Assert.DoesNotContain("ConvertTo-Json -Depth", content, StringComparison.Ordinal);
        Assert.DoesNotContain("dotnet test", content, StringComparison.Ordinal);
    }

    [Fact]
    public void ToolsArchitectureDocStatesRepositoryBoundaries()
    {
        var content = File.ReadAllText(ToolsArchitectureDocPath);

        var requiredPhrases = new[]
        {
            "src/Backend",
            "src/Frontend",
            "tests",
            "docs",
            "tools",
            "scripts",
            ".github/workflows",
            "thin wrappers only",
            "C# automation"
        };

        foreach (var requiredPhrase in requiredPhrases)
        {
            Assert.Contains(requiredPhrase, content, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static string ToolProjectPath =>
        Path.Combine(TestPaths.RepoRoot, "tools", "AssistantEngineer.Tools.EngineeringCore", "AssistantEngineer.Tools.EngineeringCore.csproj");

    private static string ToolProgramPath =>
        Path.Combine(TestPaths.RepoRoot, "tools", "AssistantEngineer.Tools.EngineeringCore", "Program.cs");

    private static string ToolReadmePath =>
        Path.Combine(TestPaths.RepoRoot, "tools", "AssistantEngineer.Tools.EngineeringCore", "README.md");

    private static string ToolsArchitectureDocPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "engineering-core", "EngineeringCoreToolsArchitecture.md");
}
