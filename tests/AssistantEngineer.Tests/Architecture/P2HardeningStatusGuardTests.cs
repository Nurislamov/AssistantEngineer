using AssistantEngineer.Tests;
using Microsoft.Extensions.Configuration;

namespace AssistantEngineer.Tests.Architecture;

public class P2HardeningStatusGuardTests
{
    [Fact]
    public void P2HardeningStatusDocExistsAndMentionsAllP2Stages()
    {
        var path = Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "architecture",
            "p2-hardening-status.md");

        Assert.True(File.Exists(path), $"P2 hardening status doc must exist: {path}");

        var content = File.ReadAllText(path);
        Assert.Contains("P2-01", content, StringComparison.Ordinal);
        Assert.Contains("P2-02", content, StringComparison.Ordinal);
        Assert.Contains("P2-03", content, StringComparison.Ordinal);
        Assert.Contains("P2-04", content, StringComparison.Ordinal);
        Assert.Contains("idempotency", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("pagination", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("structured logging", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EngineeringWorkflowListEndpointsArePaged()
    {
        var controllerPath = Path.Combine(
            TestPaths.ApiProjectPath,
            "Controllers",
            "Calculations",
            "EngineeringWorkflowController.cs");

        var text = File.ReadAllText(controllerPath);

        Assert.Contains("PagedResponse<EngineeringCalculationJobResultDto>", text, StringComparison.Ordinal);
        Assert.Contains("ListProjectJobs(", text, StringComparison.Ordinal);
        Assert.Contains("PagedResponse<EngineeringCalculationScenarioRecordDto>", text, StringComparison.Ordinal);
        Assert.Contains("GetProjectScenarios(", text, StringComparison.Ordinal);
        Assert.Contains(
            "CollectionQueryParameters query",
            text,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "ActionResult<IReadOnlyList<EngineeringCalculationJobResultDto>> ListProjectJobs",
            text,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "ActionResult<IReadOnlyList<EngineeringCalculationScenarioRecordDto>> GetProjectScenarios",
            text,
            StringComparison.Ordinal);
    }

    [Fact]
    public void HeavyWorkflowEndpointsAreRateLimitedAndIdempotencyAware()
    {
        var controllerPath = Path.Combine(
            TestPaths.ApiProjectPath,
            "Controllers",
            "Calculations",
            "EngineeringWorkflowController.cs");
        var submissionServicePath = Path.Combine(
            TestPaths.ApiProjectPath,
            "Services",
            "Calculations",
            "Workflow",
            "EngineeringWorkflowSubmissionService.cs");

        var text = File.ReadAllText(controllerPath);
        var submissionText = File.ReadAllText(submissionServicePath);

        Assert.Contains("[EnableRateLimiting(ApiHardeningRegistration.EngineeringHeavyPolicyName)]", text, StringComparison.Ordinal);
        Assert.Contains("RunCalculation(", text, StringComparison.Ordinal);
        Assert.Contains("CreateOrRunJob(", text, StringComparison.Ordinal);
        Assert.Contains("ExportReportJson(", text, StringComparison.Ordinal);
        Assert.Contains("ExportReportMarkdown(", text, StringComparison.Ordinal);

        Assert.Contains("Idempotency-Key", text, StringComparison.Ordinal);
        Assert.Contains("IEngineeringWorkflowSubmissionService", text, StringComparison.Ordinal);
        Assert.Contains("IEngineeringIdempotencyService", submissionText, StringComparison.Ordinal);
        Assert.Contains("EvaluateAsync", submissionText, StringComparison.Ordinal);
        Assert.Contains("RecordSuccessAsync", submissionText, StringComparison.Ordinal);
    }

    [Fact]
    public void CalculationAndJobServicesUseStructuredLoggerDependencies()
    {
        var runner = File.ReadAllText(Path.Combine(
            TestPaths.ApiProjectPath,
            "Services",
            "Calculations",
            "EngineeringCalculationScenarioRunner.cs"));
        var moduleExecutor = File.ReadAllText(Path.Combine(
            TestPaths.ApiProjectPath,
            "Services",
            "Calculations",
            "ScenarioExecution",
            "EngineeringCalculationScenarioModuleExecutor.cs"));
        var jobService = File.ReadAllText(Path.Combine(
            TestPaths.ApiProjectPath,
            "Services",
            "Calculations",
            "EngineeringCalculationJobService.cs"));
        var idempotencyService = File.ReadAllText(Path.Combine(
            TestPaths.ApiProjectPath,
            "Services",
            "Calculations",
            "Idempotency",
            "InMemoryEngineeringIdempotencyService.cs"));

        Assert.Contains("ILogger<EngineeringCalculationScenarioRunner>", runner, StringComparison.Ordinal);
        Assert.Contains("ILogger<EngineeringCalculationScenarioModuleExecutor>", moduleExecutor, StringComparison.Ordinal);
        Assert.Contains("ILogger<EngineeringCalculationJobService>", jobService, StringComparison.Ordinal);
        Assert.Contains("ILogger<InMemoryEngineeringIdempotencyService>", idempotencyService, StringComparison.Ordinal);
    }

    [Fact]
    public void HealthAndReadinessEndpointsRemainMappedAndAnonymous()
    {
        var pipelinePath = Path.Combine(
            TestPaths.ApiProjectPath,
            "Configuration",
            "ApiPipelineConfiguration.cs");

        var text = File.ReadAllText(pipelinePath);

        Assert.Contains("MapHealthChecks(\"/health\"", text, StringComparison.Ordinal);
        Assert.Contains("MapHealthChecks(\"/ready\"", text, StringComparison.Ordinal);
        Assert.Contains(".AllowAnonymous()", text, StringComparison.Ordinal);
    }

    [Fact]
    public void DefaultCorsRemainsDenyByDefaultWithoutWildcard()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.json"), optional: false)
            .Build();

        var origins = configuration
            .GetSection("ApiHardening:Cors:AllowedOrigins")
            .Get<string[]>() ?? [];

        Assert.DoesNotContain(origins, item => item == "*");
    }

    [Fact]
    public void DefaultPayloadLimitsRemainEnabled()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.json"), optional: false)
            .Build();

        Assert.True(configuration.GetValue<bool>("EngineeringWorkflowPersistence:PayloadLimits:Enabled"));
        Assert.True(configuration.GetValue<int>("EngineeringWorkflowPersistence:PayloadLimits:RequestJsonMaxBytes") > 0);
        Assert.True(configuration.GetValue<int>("EngineeringWorkflowPersistence:PayloadLimits:ArtifactContentMaxBytes") > 0);
    }

    [Fact]
    public void FrontendTestBaselineFilesAndScriptsExist()
    {
        var packageJsonPath = Path.Combine(TestPaths.RepoRoot, "src", "Frontend", "package.json");
        Assert.True(File.Exists(packageJsonPath), $"Frontend package.json must exist: {packageJsonPath}");

        var packageJson = File.ReadAllText(packageJsonPath);
        Assert.Contains("\"test\"", packageJson, StringComparison.Ordinal);
        Assert.Contains("vitest", packageJson, StringComparison.OrdinalIgnoreCase);

        Assert.True(File.Exists(Path.Combine(TestPaths.RepoRoot, "src", "Frontend", "src", "test", "setup.ts")));
        Assert.True(File.Exists(Path.Combine(TestPaths.RepoRoot, "src", "Frontend", "src", "entities", "engineering-workflow", "api", "engineeringWorkflowClient.test.ts")));
        Assert.True(File.Exists(Path.Combine(TestPaths.RepoRoot, "src", "Frontend", "src", "widgets", "engineering-workflow", "ui", "WorkflowDiagnosticsPanel.test.tsx")));
    }
}
