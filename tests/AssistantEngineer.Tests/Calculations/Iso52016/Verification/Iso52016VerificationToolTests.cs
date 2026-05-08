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
        var program = ReadToolSources();

        Assert.Contains("verify-all", program);
        Assert.Contains("assert-release-ready", program);
        Assert.Contains("verify-stage", program);
        Assert.Contains("list-stages", program);
    }

    [Fact]
    public void Program_ReadsRegistryAndOwnsVerificationPolicies()
    {
        var program = ReadToolSources();

        Assert.Contains("Iso52016VerificationRegistry.json", program);
        Assert.Contains("VerifyGeneratedArtifactPolicy", program);
        Assert.Contains("VerifyClaimBoundaries", program);
        Assert.Contains("VerifyNoPositiveEquivalenceClaims", program);
        Assert.Contains("VerifyWrapperScripts", program);
        Assert.Contains("RunStageTests", program);
    }

    [Fact]
    public void Program_ChecksGeneratedArtifactsAndClaimBoundariesInCSharp()
    {
        var program = ReadToolSources();

        Assert.Contains("git", program);
        Assert.Contains("ls-files", program);
        Assert.Contains("generated artifact", program, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("validation/internal engineering anchors only", program, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ForbiddenPositiveClaims", program);
    }

    [Fact]
    public void Program_RemainsThinCompositionRoot()
    {
        var path = BuildPath("Program.cs");
        var lines = File.ReadAllLines(path);
        Assert.True(lines.Length <= 180, $"Program.cs should stay thin (actual lines: {lines.Length}).");

        var program = File.ReadAllText(path);
        Assert.Contains("Iso52016VerificationRunner", program, StringComparison.Ordinal);
        Assert.Contains("Iso52016VerificationCommandOptions", program, StringComparison.Ordinal);
    }

    private static string ReadToolSources() =>
        string.Join(
            Environment.NewLine,
            File.ReadAllText(BuildPath("Program.cs")),
            File.ReadAllText(BuildPath("Iso52016VerificationRunner.cs")),
            File.ReadAllText(BuildPath("Iso52016VerificationCommandOptions.cs")));

    private static string BuildPath(string fileName) =>
        Path.Combine(
            TestPaths.RepoRoot,
            "tools",
            "AssistantEngineer.Tools.Iso52016Verification",
            fileName);

    private static string ReadRepoFile(params string[] parts)
    {
        var path = Path.Combine(parts.Prepend(TestPaths.RepoRoot).ToArray());

        Assert.True(File.Exists(path), $"Expected file does not exist: {path}");
        return File.ReadAllText(path);
    }
}
