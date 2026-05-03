using AssistantEngineer.Tests;

namespace AssistantEngineer.Tests.Tools;

public class EngineeringCoreVerificationToolArchitectureTests
{
    [Fact]
    public void EngineeringCoreVerificationToolProjectReadmeAndArchitectureDocExist()
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
            Assert.True(File.Exists(requiredFile), $"Required Engineering Core verification artifact is missing: {requiredFile}");
        }
    }

    [Fact]
    public void EngineeringCoreVerificationToolOwnsVerificationSequence()
    {
        var content = File.ReadAllText(ToolProgramPath);

        var requiredPhrases = new[]
        {
            "Engineering Core V1 verification",
            "Frontend TypeScript/Vite build",
            "Engineering Core release evidence package guard tests",
            "Engineering Core API contract snapshot guard tests",
            "EnergyPlus validation evidence package tests",
            "Engineering Core traceability matrix guard tests",
            "EnergyPlus/ASHRAE 140 validation harness guard tests",
            "Full backend test suite",
            "skip-frontend",
            "skip-full-dotnet",
            "fast"
        };

        foreach (var requiredPhrase in requiredPhrases)
        {
            Assert.Contains(requiredPhrase, content, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void VerifyEngineeringCoreV1ScriptIsThinWrapper()
    {
        var content = File.ReadAllText(WrapperScriptPath);

        Assert.Contains("AssistantEngineer.Tools.EngineeringCoreVerification.csproj", content, StringComparison.Ordinal);
        Assert.Contains("dotnet run --project", content, StringComparison.Ordinal);
        Assert.Contains("--skip-frontend", content, StringComparison.Ordinal);
        Assert.Contains("--skip-full-dotnet", content, StringComparison.Ordinal);
        Assert.Contains("--fast", content, StringComparison.Ordinal);

        Assert.DoesNotContain("Frontend TypeScript/Vite build", content, StringComparison.Ordinal);
        Assert.DoesNotContain("Engineering Core status and formula audit tests", content, StringComparison.Ordinal);
        Assert.DoesNotContain("dotnet test .\\AssistantEngineer.sln --filter", content, StringComparison.Ordinal);
        Assert.DoesNotContain("function Invoke-Step", content, StringComparison.Ordinal);
    }

    [Fact]
    public void ArchitectureDocDocumentsToolAndWrapperBoundary()
    {
        var content = File.ReadAllText(ArchitectureDocPath);

        var requiredPhrases = new[]
        {
            "Engineering Core Verification Tool Architecture",
            "C# tool",
            "thin wrapper",
            "verification step list",
            "tools/AssistantEngineer.Tools.EngineeringCoreVerification"
        };

        foreach (var requiredPhrase in requiredPhrases)
        {
            Assert.Contains(requiredPhrase, content, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static string ToolProjectPath =>
        Path.Combine(TestPaths.RepoRoot, "tools", "AssistantEngineer.Tools.EngineeringCoreVerification", "AssistantEngineer.Tools.EngineeringCoreVerification.csproj");

    private static string ToolProgramPath =>
        Path.Combine(TestPaths.RepoRoot, "tools", "AssistantEngineer.Tools.EngineeringCoreVerification", "Program.cs");

    private static string ToolReadmePath =>
        Path.Combine(TestPaths.RepoRoot, "tools", "AssistantEngineer.Tools.EngineeringCoreVerification", "README.md");

    private static string ArchitectureDocPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "engineering-core", "EngineeringCoreVerificationToolArchitecture.md");

    private static string WrapperScriptPath =>
        Path.Combine(TestPaths.RepoRoot, "scripts", "engineering-core", "verify-engineering-core-v1.ps1");
}
