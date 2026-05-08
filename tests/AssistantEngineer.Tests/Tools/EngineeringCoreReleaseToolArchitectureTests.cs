using AssistantEngineer.Tests;

namespace AssistantEngineer.Tests.Tools;

public class EngineeringCoreReleaseToolArchitectureTests
{
    [Fact]
    public void EngineeringCoreReleaseToolProjectReadmeAndArchitectureDocExist()
    {
        var requiredFiles = new[]
        {
            ToolProjectPath,
            ToolProgramPath,
            ToolReadmePath,
            ArchitectureDocPath,
            ReleaseReadyWrapperPath,
            RegenerateArtifactsWrapperPath,
            VerifyContractsWrapperPath,
            VerifyManifestWrapperPath,
            VerifySmokeWrapperPath
        };

        foreach (var requiredFile in requiredFiles)
        {
            Assert.True(File.Exists(requiredFile), $"Required Engineering Core release artifact is missing: {requiredFile}");
        }
    }

    [Fact]
    public void EngineeringCoreReleaseToolOwnsReleaseProfileCommands()
    {
        var content = File.ReadAllText(ToolProgramPath);

        var requiredPhrases = new[]
        {
            "regenerate-artifacts",
            "verify-smoke",
            "verify-contracts",
            "verify-manifest",
            "assert-release-ready",
            "Required release readiness files are missing",
            "FormulaAuditMatrix",
            "EngineeringCoreV1ReleaseManifestTests",
            "EngineeringCoreV1TraceabilityMatrixTests",
            "EngineeringCoreV1ReportContractSnapshotTests"
        };

        foreach (var requiredPhrase in requiredPhrases)
        {
            Assert.Contains(requiredPhrase, content, StringComparison.Ordinal);
        }
    }

    [Theory]
    [InlineData("assert-engineering-core-v1-release-ready.ps1", "assert-release-ready")]
    [InlineData("regenerate-engineering-core-v1-artifacts.ps1", "regenerate-artifacts")]
    [InlineData("verify-engineering-core-v1-contracts.ps1", "verify-contracts")]
    [InlineData("verify-engineering-core-v1-manifest.ps1", "verify-manifest")]
    [InlineData("verify-engineering-core-v1-smoke.ps1", "verify-smoke")]
    public void ReleaseProfileScriptsAreThinWrappers(string scriptName, string command)
    {
        var scriptPath = Path.Combine(TestPaths.RepoRoot, "scripts", "engineering-core", scriptName);

        Assert.True(File.Exists(scriptPath), $"Expected wrapper script to exist: {scriptPath}");

        var content = File.ReadAllText(scriptPath);

        Assert.Contains("AssistantEngineer.Tools.EngineeringCoreRelease.csproj", content, StringComparison.Ordinal);
        Assert.Contains(command, content, StringComparison.Ordinal);
        Assert.Contains("dotnet run --project", content, StringComparison.Ordinal);

        Assert.DoesNotContain("function Invoke-Step", content, StringComparison.Ordinal);
        Assert.DoesNotContain("function Assert-FileExists", content, StringComparison.Ordinal);
        Assert.DoesNotContain("dotnet test .\\AssistantEngineer.sln --filter", content, StringComparison.Ordinal);
        Assert.DoesNotContain("Required release readiness artifacts", content, StringComparison.Ordinal);
    }

    [Fact]
    public void ArchitectureDocDocumentsReleaseToolBoundaryAndNonClaims()
    {
        var content = File.ReadAllText(ArchitectureDocPath);

        var requiredPhrases = new[]
        {
            "Engineering Core Release Tool Architecture",
            "PowerShell scripts",
            "thin wrappers",
            "release readiness file list",
            "verification step list",
            "artifact regeneration sequence",
            "does not claim exact EnergyPlus numerical equivalence",
            "does not claim ASHRAE 140 / BESTEST-style validation anchor coverage"
        };

        foreach (var requiredPhrase in requiredPhrases)
        {
            Assert.Contains(requiredPhrase, content, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static string ToolProjectPath =>
        Path.Combine(TestPaths.RepoRoot, "tools", "AssistantEngineer.Tools.EngineeringCoreRelease", "AssistantEngineer.Tools.EngineeringCoreRelease.csproj");

    private static string ToolProgramPath =>
        Path.Combine(TestPaths.RepoRoot, "tools", "AssistantEngineer.Tools.EngineeringCoreRelease", "Program.cs");

    private static string ToolReadmePath =>
        Path.Combine(TestPaths.RepoRoot, "tools", "AssistantEngineer.Tools.EngineeringCoreRelease", "README.md");

    private static string ArchitectureDocPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "engineering-core", "EngineeringCoreReleaseToolArchitecture.md");

    private static string ReleaseReadyWrapperPath =>
        Path.Combine(TestPaths.RepoRoot, "scripts", "engineering-core", "assert-engineering-core-v1-release-ready.ps1");

    private static string RegenerateArtifactsWrapperPath =>
        Path.Combine(TestPaths.RepoRoot, "scripts", "engineering-core", "regenerate-engineering-core-v1-artifacts.ps1");

    private static string VerifyContractsWrapperPath =>
        Path.Combine(TestPaths.RepoRoot, "scripts", "engineering-core", "verify-engineering-core-v1-contracts.ps1");

    private static string VerifyManifestWrapperPath =>
        Path.Combine(TestPaths.RepoRoot, "scripts", "engineering-core", "verify-engineering-core-v1-manifest.ps1");

    private static string VerifySmokeWrapperPath =>
        Path.Combine(TestPaths.RepoRoot, "scripts", "engineering-core", "verify-engineering-core-v1-smoke.ps1");
}
