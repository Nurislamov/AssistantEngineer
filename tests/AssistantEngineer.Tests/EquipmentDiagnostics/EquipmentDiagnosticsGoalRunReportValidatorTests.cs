using System.Diagnostics;
using System.Text.Json;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Verification;

namespace AssistantEngineer.Tests.EquipmentDiagnostics;

[Collection(EquipmentDiagnosticsVerificationCliCollection.Name)]
public class EquipmentDiagnosticsGoalRunReportValidatorTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public void ValidMinimalReportPasses()
    {
        var result = Validate(ValidReport());

        Assert.True(result.IsReady);
        Assert.Equal("PASS", result.Status);
        Assert.Empty(result.Blockers);
    }

    [Fact]
    public void MissingRequiredFieldsFail()
    {
        var report = ValidReport() with { GoalId = null, Scope = [], Constraints = [] };

        var result = Validate(report);

        Assert.False(result.IsReady);
        Assert.Contains(result.Blockers, blocker => blocker.Contains("goalId", StringComparison.Ordinal));
        Assert.Contains(result.Blockers, blocker => blocker.Contains("scope", StringComparison.Ordinal));
        Assert.Contains(result.Blockers, blocker => blocker.Contains("constraints", StringComparison.Ordinal));
    }

    [Fact]
    public void FailedPreflightCommandCreatesBlocker()
    {
        var report = ValidReport() with
        {
            Preflight = new([new("Build", "dotnet build", "fail", "Build failed.")])
        };

        Assert.Contains(Validate(report).Blockers, blocker =>
            blocker.Contains("Preflight command failed", StringComparison.Ordinal));
    }

    [Fact]
    public void FailedPhaseCreatesBlocker()
    {
        var phase = ValidReport().Phases!.Single() with { Status = "fail" };
        var report = ValidReport() with { Phases = [phase] };

        Assert.Contains(Validate(report).Blockers, blocker =>
            blocker.Contains("Phase 1 failed", StringComparison.Ordinal));
    }

    [Fact]
    public void FinalAuditFailCreatesBlocker()
    {
        var report = ValidReport() with { FinalAudit = ValidReport().FinalAudit! with { Status = "fail" } };

        Assert.Contains(Validate(report).Blockers, blocker =>
            blocker.Contains("Final audit failed", StringComparison.Ordinal));
    }

    [Fact]
    public void UnsupportedStatusCreatesBlocker()
    {
        var phase = ValidReport().Phases!.Single() with { Status = "complete" };
        var report = ValidReport() with { Phases = [phase] };

        Assert.Contains(Validate(report).Blockers, blocker =>
            blocker.Contains("status must be pass, fail, or not_run", StringComparison.Ordinal));
    }

    [Fact]
    public void ForbiddenClaimsCreateBlocker()
    {
        var report = ValidReport() with { Title = string.Concat("Claims ", "production ", "ready") };

        Assert.Contains(Validate(report).Blockers, blocker =>
            blocker.Contains("forbidden claim", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("docs/generated-goal-run.json")]
    [InlineData("artifacts/verification/service-manual.pdf")]
    [InlineData("artifacts/verification/raw.log")]
    [InlineData("artifacts/verification/secret-output.json")]
    public void UnsafeGeneratedArtifactPathsAreRejected(string path)
    {
        var report = ValidReport() with { GeneratedArtifacts = [path] };

        Assert.Contains(Validate(report).Blockers, blocker =>
            blocker.Contains("Generated artifact", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CliCommandReturnsSuccessForValidFixture()
    {
        var result = RunCli("valid-goal-run-report.json");

        Assert.True(result.ExitCode == 0, result.FailureDetails);
        Assert.Contains("PASS", result.Output, StringComparison.Ordinal);
        Assert.Contains("Blockers: 0", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void CliCommandReturnsFailureForInvalidFixture()
    {
        var result = RunCli("invalid-goal-run-report.json");

        Assert.True(result.ExitCode != 0, result.FailureDetails);
        Assert.Contains("FAIL", result.Output, StringComparison.Ordinal);
        Assert.Contains("Blockers:", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void ValidatorDocsSchemaAndSafetyStatementsExist()
    {
        var docsRoot = Path.Combine(TestPaths.RepoRoot, "docs", "engineering-workflow");
        var validatorDoc = File.ReadAllText(Path.Combine(docsRoot, "goal-run-report-validator.md"));
        Assert.True(File.Exists(Path.Combine(docsRoot, "goal-run-report.schema.json")));
        foreach (var statement in new[]
                 {
                     "no runtime AI agent",
                     "no RAG/vector search",
                     "no Telegram command execution",
                     "no production/public release claim",
                     "Generated artifacts are not committed",
                     "scripts/engineering-core/verify-engineering-core-v1.ps1"
                 })
        {
            Assert.Contains(statement, validatorDoc, StringComparison.OrdinalIgnoreCase);
        }

        var gitIgnore = File.ReadAllText(Path.Combine(TestPaths.RepoRoot, ".gitignore"));
        Assert.Contains("/artifacts/", gitIgnore, StringComparison.OrdinalIgnoreCase);
    }

    private static AssistantEngineerGoalRunValidationResult Validate(AssistantEngineerGoalRunReport report) =>
        new AssistantEngineerGoalRunReportValidator().Validate(report);

    private static AssistantEngineerGoalRunReport ValidReport() =>
        JsonSerializer.Deserialize<AssistantEngineerGoalRunReport>(
            File.ReadAllText(FixturePath("valid-goal-run-report.json")),
            JsonOptions)!;

    private static (int ExitCode, string Output, string FailureDetails) RunCli(string fixture)
    {
        var toolProject = Path.Combine(
            TestPaths.RepoRoot,
            "tools",
            "AssistantEngineer.Tools.EquipmentDiagnosticsVerification",
            "AssistantEngineer.Tools.EquipmentDiagnosticsVerification.csproj");
        Assert.True(File.Exists(toolProject), $"Verification tool project was not found: {toolProject}");

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = TestPaths.RepoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        foreach (var argument in new[]
                 {
                     "run",
                     "--project",
                     toolProject,
                     "--no-restore",
                     "--",
                     "goal-run-report",
                     "--repo-root",
                     TestPaths.RepoRoot,
                     "--input",
                     FixturePath(fixture)
                 })
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start goal-run validator CLI.");
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        var command = $"dotnet {string.Join(" ", startInfo.ArgumentList.Select(QuoteArgument))}";
        var combinedOutput = output + error;
        return (process.ExitCode, combinedOutput, $"Command: {command}{Environment.NewLine}{combinedOutput}");
    }

    private static string QuoteArgument(string argument) =>
        argument.Contains(' ') ? $"\"{argument}\"" : argument;

    private static string FixturePath(string name) =>
        Path.Combine(TestPaths.RepoRoot, "tests", "AssistantEngineer.Tests", "TestData", "EquipmentDiagnostics", name);
}
