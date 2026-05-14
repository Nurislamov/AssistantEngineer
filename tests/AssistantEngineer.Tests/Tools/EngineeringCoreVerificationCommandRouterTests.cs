using AssistantEngineer.Tools.EngineeringCoreVerification;

namespace AssistantEngineer.Tests.Tools;

public class EngineeringCoreVerificationCommandRouterTests
{
    [Fact]
    public void VerificationOptions_Parse_RecognizesSupportedFlagsCaseInsensitively()
    {
        var options = VerificationOptions.Parse(
        [
            "--skip-frontend",
            "-skipfulldotnet",
            "--FAST",
            "-NoRestore",
            "--no-build"
        ]);

        Assert.True(options.SkipFrontend);
        Assert.True(options.SkipFullDotnet);
        Assert.True(options.Fast);
        Assert.True(options.NoRestore);
        Assert.True(options.NoBuild);
    }

    [Fact]
    public void CommandRouter_BuildSteps_ControlsFrontendStepBySkipFrontendFlag()
    {
        var withFrontend = EngineeringCoreVerificationCommandRouter.BuildSteps(
            new VerificationOptions(false, false, false, false, false));

        Assert.Equal("Frontend TypeScript/Vite build", withFrontend[0].Name);

        var withoutFrontend = EngineeringCoreVerificationCommandRouter.BuildSteps(
            new VerificationOptions(true, false, false, false, false));

        Assert.DoesNotContain(withoutFrontend, step => step.Name == "Frontend TypeScript/Vite build");
    }

    [Theory]
    [InlineData("help")]
    [InlineData("-h")]
    [InlineData("--help")]
    public void CommandRouter_IsHelpRequested_RecognizesHelpAliases(string helpAlias)
    {
        Assert.True(EngineeringCoreVerificationCommandRouter.IsHelpRequested([helpAlias]));
    }

    [Fact]
    public void CommandHandler_PropagatesNonZeroExitCode()
    {
        var reportWriter = new EngineeringCoreVerificationReportWriter();
        var handler = new EngineeringCoreVerificationCommandHandler(new FakeProcessRunner(7), reportWriter);

        var result = handler.Execute(new VerificationStep("failing step", "dotnet", "--info"));

        Assert.Equal(7, result.ExitCode);
        Assert.Equal("failing step", result.Name);
    }

    [Fact]
    public void DiagnosticsFormatter_BuildSummaryLines_IsDeterministic()
    {
        var results = new[]
        {
            new StepResult("Step A", 0, TimeSpan.FromMilliseconds(500)),
            new StepResult("Step B", 1, TimeSpan.FromMilliseconds(2500)),
            new StepResult("Step C", 0, TimeSpan.FromMilliseconds(1500))
        };

        var lines = EngineeringCoreVerificationDiagnosticsFormatter.BuildSummaryLines(results);

        Assert.Equal("Verification summary:", lines[0]);
        Assert.Equal("- OK   Step A (00.500s)", lines[1]);
        Assert.Equal("- FAIL Step B (02.500s)", lines[2]);
        Assert.Equal("- OK   Step C (01.500s)", lines[3]);
        Assert.Equal("Total duration: 04.500s", lines[4]);
        Assert.Equal("Slowest 5 steps:", lines[5]);
        Assert.Equal("- Step B: 02.500s", lines[6]);
        Assert.Equal("- Step C: 01.500s", lines[7]);
        Assert.Equal("- Step A: 00.500s", lines[8]);
    }

    [Fact]
    public void PolicyGuards_ThrowsWhenExternalComparisonFoundationFilesAreMissing()
    {
        var root = Path.Combine(Path.GetTempPath(), "ae-tool-guard-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            var fileSystem = new EngineeringCoreVerificationFileSystem();
            var guards = new EngineeringCoreVerificationPolicyGuards(fileSystem);

            var exception = Assert.Throws<InvalidOperationException>(
                () => guards.AssertExternalComparisonWorkflowFoundation(root));

            Assert.Contains("External comparison workflow foundation files are missing", exception.Message, StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    private sealed class FakeProcessRunner(int exitCode) : IEngineeringCoreVerificationProcessRunner
    {
        public int RunProcess(string fileName, string arguments) => exitCode;
    }
}
