namespace AssistantEngineer.Tests.Calculations.Iso52016.Verification;

public class Iso52016VerificationToolTests
{
    [Fact]
    public void ToolProject_ExistsAndTargetsNet10()
    {
        var project = ReadRepoFile(
            "tools",
            "AssistantEngineer.Tools.Iso52016Verification",
            "AssistantEngineer.Tools.Iso52016Verification.csproj");

        Assert.Contains("<OutputType>Exe</OutputType>", project);
        Assert.Contains("<TargetFramework>net10.0</TargetFramework>", project);
        Assert.Contains("<TreatWarningsAsErrors>true</TreatWarningsAsErrors>", project);
    }

    [Fact]
    public void Program_SupportsRequiredCommands()
    {
        var program = ReadProgram();

        Assert.Contains("verify-all", program);
        Assert.Contains("assert-release-ready", program);
        Assert.Contains("verify-stage", program);
        Assert.Contains("list-stages", program);
    }

    [Fact]
    public void Program_ReadsRegistryAndOwnsVerificationPolicies()
    {
        var program = ReadProgram();

        Assert.Contains("Iso52016VerificationRegistry.json", program);
        Assert.Contains("VerifyGeneratedArtifactPolicy", program);
        Assert.Contains("VerifyClaimBoundaries", program);
        Assert.Contains("VerifyNoPositiveParityClaims", program);
        Assert.Contains("VerifyWrapperScripts", program);
        Assert.Contains("RunStageTests", program);
    }

    [Fact]
    public void Program_ChecksGeneratedArtifactsAndClaimBoundariesInCSharp()
    {
        var program = ReadProgram();

        Assert.Contains("git", program);
        Assert.Contains("ls-files", program);
        Assert.Contains("generated artifact", program, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("validation/internal engineering anchors only", program, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ForbiddenPositiveClaims", program);
    }

    private static string ReadProgram() =>
        ReadRepoFile(
            "tools",
            "AssistantEngineer.Tools.Iso52016Verification",
            "Program.cs");

    private static string ReadRepoFile(params string[] parts)
    {
        var path = Path.Combine(parts.Prepend(TestPaths.RepoRoot).ToArray());

        Assert.True(File.Exists(path), $"Expected file does not exist: {path}");
        return File.ReadAllText(path);
    }
}
