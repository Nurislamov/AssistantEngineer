using System.Diagnostics;
using System.Text.Json;

namespace AssistantEngineer.Tests.Calculations.Governance;

public sealed class EngineeringGovernanceToolTests
{
    private static readonly string ToolProjectPath = Path.Combine(
        TestPaths.RepoRoot,
        "tools",
        "AssistantEngineer.Tools.EngineeringGovernance",
        "AssistantEngineer.Tools.EngineeringGovernance.csproj");

    [Fact]
    public void VerifyReleaseReadiness_CommandExitsZero()
    {
        var result = RunTool("verify-release-readiness --repo-root .");
        Assert.Equal(0, result.ExitCode);
    }

    [Fact]
    public void VerifyClaims_CommandExitsZero()
    {
        var result = RunTool("verify-claims --repo-root .");
        Assert.Equal(0, result.ExitCode);
    }

    [Fact]
    public void InvalidArgs_ExitNonZero()
    {
        var result = RunTool("unknown-command");
        Assert.NotEqual(0, result.ExitCode);
    }

    [Fact]
    public void WriteStatusSample_ProducesStableJsonShape()
    {
        var outputPath = Path.Combine("artifacts", "generated", "governance-tests", "status-sample-tool-test.json");
        var result = RunTool($"write-status-sample --repo-root . --output {outputPath}");
        Assert.Equal(0, result.ExitCode);

        var absolutePath = Path.Combine(TestPaths.RepoRoot, outputPath.Replace('/', Path.DirectorySeparatorChar));
        Assert.True(File.Exists(absolutePath));

        using var document = JsonDocument.Parse(File.ReadAllText(absolutePath));
        var root = document.RootElement;

        Assert.True(root.TryGetProperty("releaseReadinessId", out _));
        Assert.True(root.TryGetProperty("status", out _));
        Assert.True(root.TryGetProperty("claimBoundary", out _));
        Assert.True(root.TryGetProperty("defaultBehavior", out _));
        Assert.True(root.TryGetProperty("optInFlags", out _));
    }

    private static (int ExitCode, string Output) RunTool(string arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{ToolProjectPath}\" -- {arguments}",
            WorkingDirectory = TestPaths.RepoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start governance tool process.");
        var output = process.StandardOutput.ReadToEnd();
        output += process.StandardError.ReadToEnd();
        process.WaitForExit();

        return (process.ExitCode, output);
    }
}
