using AssistantEngineer.Api.Contracts.Calculations;
using AssistantEngineer.Api.Services.Calculations;
using AssistantEngineer.Api.Services.Calculations.Persistence;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Tests.Calculations;

public class EngineeringCalculationJobServiceTests
{
    [Fact]
    public async Task EngineeringCalculationJobCreateQueuedPersistsQueuedLifecycle()
    {
        var fixture = CreateFixture();
        fixture.Runner.ResultFactory = request => CreateScenarioResult(request.ScenarioId, EngineeringCalculationExecutionStatus.Completed);

        var request = CreateJobRequest("job-queued", "scenario-queued", EngineeringCalculationJobExecutionMode.Queued);
        var result = await fixture.Service.CreateOrRunJobAsync(request, CancellationToken.None);

        Assert.Equal("job-queued", result.JobId);
        Assert.Equal(EngineeringCalculationJobStatus.Queued, result.Status);
        Assert.DoesNotContain(result.Diagnostics, item => item.Code == "CALCULATION_JOB_WORKER_NOT_ENABLED");
        Assert.True(result.HistoryEvents.Count >= 2);
        Assert.Equal(0, fixture.Runner.InvocationCount);
    }

    [Fact]
    public async Task EngineeringCalculationJobExecuteQueuedRunsExistingJobAndStoresScenarioArtifacts()
    {
        var fixture = CreateFixture();
        fixture.Runner.ResultFactory = request => CreateScenarioResult(request.ScenarioId, EngineeringCalculationExecutionStatus.Completed);

        var queued = await fixture.Service.CreateOrRunJobAsync(
            CreateJobRequest("job-worker", "scenario-worker", EngineeringCalculationJobExecutionMode.Queued),
            CancellationToken.None);

        Assert.Equal(EngineeringCalculationJobStatus.Queued, queued.Status);
        Assert.Equal(0, fixture.Runner.InvocationCount);

        var completed = await fixture.Service.ExecuteQueuedJobAsync("job-worker", CancellationToken.None);

        Assert.NotNull(completed);
        Assert.Equal(EngineeringCalculationJobStatus.Completed, completed.Status);
        Assert.NotNull(completed.ScenarioResultSummary);
        Assert.True(fixture.Runner.InvocationCount > 0);
        Assert.Contains(completed.HistoryEvents, item => item.Status == EngineeringCalculationJobStatus.Running);
        Assert.Contains(completed.HistoryEvents, item => item.Status == EngineeringCalculationJobStatus.Completed);
        Assert.NotEmpty(completed.PersistedArtifactReferences);
    }

    [Fact]
    public async Task EngineeringCalculationJobExecuteClaimedSkipsWhenWorkerDoesNotOwnClaim()
    {
        var fixture = CreateFixture();
        fixture.Runner.ResultFactory = request => CreateScenarioResult(request.ScenarioId, EngineeringCalculationExecutionStatus.Completed);
        await fixture.Service.CreateOrRunJobAsync(
            CreateJobRequest("job-claim-mismatch", "scenario-claim-mismatch", EngineeringCalculationJobExecutionMode.Queued),
            CancellationToken.None);

        var claimedByWorkerA = await fixture.JobRepository.TryClaimQueuedJobAsync(
            "job-claim-mismatch",
            "worker-a",
            TimeSpan.FromSeconds(180),
            CancellationToken.None);
        Assert.NotNull(claimedByWorkerA);

        var skipped = await fixture.Service.ExecuteClaimedJobAsync(
            "job-claim-mismatch",
            "worker-b",
            CancellationToken.None);

        Assert.NotNull(skipped);
        Assert.Equal(EngineeringCalculationJobStatus.Running, skipped!.Status);
        Assert.Equal(0, fixture.Runner.InvocationCount);
    }

    [Fact]
    public async Task EngineeringCalculationJobExecuteClaimedSkipsWhenJobIsNotRunning()
    {
        var fixture = CreateFixture();
        fixture.Runner.ResultFactory = request => CreateScenarioResult(request.ScenarioId, EngineeringCalculationExecutionStatus.Completed);
        await fixture.Service.CreateOrRunJobAsync(
            CreateJobRequest("job-not-running", "scenario-not-running", EngineeringCalculationJobExecutionMode.Queued),
            CancellationToken.None);

        var skipped = await fixture.Service.ExecuteClaimedJobAsync(
            "job-not-running",
            "worker-a",
            CancellationToken.None);

        Assert.NotNull(skipped);
        Assert.Equal(EngineeringCalculationJobStatus.Queued, skipped!.Status);
        Assert.Equal(0, fixture.Runner.InvocationCount);
    }

    [Fact]
    public async Task EngineeringCalculationJobSynchronousExecutesRunnerAndStoresScenarioArtifacts()
    {
        var fixture = CreateFixture();
        fixture.Runner.ResultFactory = request => CreateScenarioResult(request.ScenarioId, EngineeringCalculationExecutionStatus.CompletedWithWarnings);

        var request = CreateJobRequest("job-sync", "scenario-sync", EngineeringCalculationJobExecutionMode.Synchronous);
        var result = await fixture.Service.CreateOrRunJobAsync(request, CancellationToken.None);

        Assert.Equal(EngineeringCalculationJobStatus.CompletedWithWarnings, result.Status);
        Assert.Equal(100, result.ProgressPercent);
        Assert.NotNull(result.ScenarioResultSummary);
        Assert.True(fixture.Runner.InvocationCount > 0);

        var persisted = await fixture.Service.GetJobAsync("job-sync", CancellationToken.None);
        Assert.NotNull(persisted);
        Assert.Equal("scenario-sync", persisted.ScenarioId);
        Assert.NotEmpty(persisted.PersistedArtifactReferences);
    }

    [Fact]
    public async Task EngineeringCalculationJobCancelQueuedTransitionsToCancelled()
    {
        var fixture = CreateFixture();
        fixture.Runner.ResultFactory = request => CreateScenarioResult(request.ScenarioId, EngineeringCalculationExecutionStatus.Completed);

        var queued = await fixture.Service.CreateOrRunJobAsync(
            CreateJobRequest("job-cancel", "scenario-cancel", EngineeringCalculationJobExecutionMode.Queued),
            CancellationToken.None);
        Assert.Equal(EngineeringCalculationJobStatus.Queued, queued.Status);

        var cancelled = await fixture.Service.CancelJobAsync("job-cancel", CancellationToken.None);
        Assert.NotNull(cancelled);
        Assert.Equal(EngineeringCalculationJobStatus.Cancelled, cancelled.Status);
        Assert.Equal(100, cancelled.ProgressPercent);
    }

    [Fact]
    public async Task EngineeringCalculationJobServiceProducesFailedExecutionOnRunnerException()
    {
        var fixture = CreateFixture();
        fixture.Runner.ExceptionFactory = _ => new InvalidOperationException("runner failure");

        var result = await fixture.Service.CreateOrRunJobAsync(
            CreateJobRequest("job-failed", "scenario-failed", EngineeringCalculationJobExecutionMode.Synchronous),
            CancellationToken.None);

        Assert.Equal(EngineeringCalculationJobStatus.FailedExecution, result.Status);
        Assert.Contains(result.Diagnostics, item => item.Code == "CALCULATION_JOB_EXECUTION_FAILED");
        Assert.Equal(100, result.ProgressPercent);
    }

    private static Fixture CreateFixture()
    {
        var store = new EngineeringWorkflowMemoryStore();
        var projectRepository = new InMemoryEngineeringProjectRepository(store);
        var workflowRepository = new InMemoryEngineeringWorkflowStateRepository(store);
        var scenarioRepository = new InMemoryEngineeringCalculationScenarioRepository(store);
        var artifactRepository = new InMemoryEngineeringCalculationArtifactRepository(store);
        var historyRepository = new InMemoryEngineeringScenarioHistoryRepository(store);
        var jobRepository = new InMemoryEngineeringCalculationJobRepository(store);
        var jobEventRepository = new InMemoryEngineeringCalculationJobEventRepository(store);
        var runner = new RunnerStub();
        var workflowPersistence = new EngineeringWorkflowPersistenceService(
            projectRepository,
            workflowRepository,
            scenarioRepository,
            artifactRepository,
            historyRepository,
            Options.Create(new EngineeringWorkflowPersistenceOptions
            {
                Provider = EngineeringWorkflowPersistenceProvider.InMemory
            }));
        var payloadCodec = new EngineeringCalculationJobPayloadCodec();
        var statusTransitionPolicy = new EngineeringCalculationJobStatusTransitionPolicy();
        var eventRecorder = new EngineeringCalculationJobEventRecorder(jobEventRepository, payloadCodec);

        var service = new EngineeringCalculationJobService(
            runner,
            workflowPersistence,
            jobRepository,
            jobEventRepository,
            payloadCodec,
            statusTransitionPolicy,
            eventRecorder,
            NullLogger<EngineeringCalculationJobService>.Instance);

        return new Fixture(service, runner, jobRepository);
    }

    private static EngineeringCalculationJobRequestDto CreateJobRequest(
        string jobId,
        string scenarioId,
        EngineeringCalculationJobExecutionMode mode)
    {
        var state = CreateState();
        return new EngineeringCalculationJobRequestDto(
            JobId: jobId,
            ProjectId: state.ProjectId,
            ScenarioId: scenarioId,
            ScenarioRequest: new EngineeringCalculationScenarioRequestDto(
                ScenarioId: scenarioId,
                ProjectId: state.ProjectId,
                BuildingId: state.BuildingId,
                ScenarioKind: EngineeringCalculationScenarioKind.FullEngineeringCore,
                ExecutionMode: EngineeringCalculationExecutionMode.ExecuteAvailableModules,
                State: state,
                RequestedModules: state.AvailableModules,
                DetailLevel: "Summary",
                IncludeTrace: true,
                IncludeReport: true,
                ReportFormats: ["Json", "Markdown"],
                DeterministicTimestampUtc: null,
                DiagnosticsMode: "Deterministic"),
            ExecutionMode: mode,
            RequestedPriority: null,
            IncludeTrace: true,
            IncludeReport: true,
            RequestedReportFormats: ["Json", "Markdown"],
            DeterministicTimestampUtc: null);
    }

    private static EngineeringWorkflowStateDto CreateState()
    {
        return new EngineeringWorkflowStateDto(
            ProjectId: 500,
            ProjectName: "Job tests project",
            BuildingId: 5000,
            CurrentStep: "Review",
            Steps: [new EngineeringWorkflowStepDto("Review", "valid", true)],
            AvailableModules: ["ThermalTopology", "SystemEnergy", "Reporting"],
            BuildingMetadata: new EngineeringWorkflowBuildingMetadataDto(
                BuildingName: "Building",
                LocationText: "Location",
                FloorAreaM2: 100,
                VolumeM3: 250,
                NumberOfZones: 1,
                Notes: "Job test state"),
            Zones: [new EngineeringWorkflowZoneDto("zone-1", "Zone 1", "Conditioned", 100, 250, "valid")],
            Boundaries: [new EngineeringWorkflowBoundaryDto("b-1", "Zone 1", "External", 25, 0.4, null, "exterior", "valid")],
            WeatherSolarSettings: new EngineeringWorkflowWeatherSolarSettingsDto("Ready", "UTC+5", "Ready"),
            VentilationSettings: new EngineeringWorkflowVentilationSettingsDto(1, "Auto", "Configured", []),
            GroundSettings: new EngineeringWorkflowGroundSettingsDto(1, "Constant", "valid"),
            DomesticHotWaterSettings: new EngineeringWorkflowDomesticHotWaterSettingsDto("PerPerson", "1200", "200", "NoDoubleCounting"),
            SystemEnergySettings: new EngineeringWorkflowSystemEnergySettingsDto("Heating,DHW", "Electricity", "Ready"),
            Diagnostics: [],
            Assumptions: ["state fixture"],
            Links: ["/api/v1/engineering-workflow/run-calculation"],
            CalculationTraceSummary: null,
            ReportSummary: null,
            Metadata: new Dictionary<string, string>());
    }

    private static EngineeringCalculationScenarioResultDto CreateScenarioResult(
        string scenarioId,
        EngineeringCalculationExecutionStatus status)
    {
        return new EngineeringCalculationScenarioResultDto(
            ScenarioId: scenarioId,
            Status: status,
            Executed: true,
            ExecutedModules: ["ThermalTopology"],
            SkippedModules: ["Ground"],
            UnavailableModules: [],
            ValidationDiagnostics:
            [
                new EngineeringWorkflowDiagnosticDto(
                    Severity: "warning",
                    Code: "JOB_TEST_WARNING",
                    Message: "fixture warning",
                    SourceStep: "Validation")
            ],
            Assumptions: ["scenario fixture assumption"],
            Warnings: ["scenario fixture warning"],
            ModuleSummaries: new EngineeringCalculationModuleSummariesDto(
                TopologySummary: "Executed.",
                VentilationSummary: "Skipped.",
                GroundSummary: "Skipped.",
                HeatingCoolingSummary: "Skipped.",
                DomesticHotWaterSummary: "Skipped.",
                SystemEnergySummary: "Executed."),
            ModuleResults:
            [
                new EngineeringCalculationModuleExecutionResultDto(
                    ModuleKind: "ThermalTopology",
                    Status: EngineeringCalculationModuleExecutionStatus.Executed,
                    SummaryValues: [new EngineeringCalculationModuleValueDto("zones", "Zone count", 1)],
                    Diagnostics: [],
                    Assumptions: [],
                    Warnings: [],
                    DurationMilliseconds: 10,
                    SourceServiceName: "runner-stub")
            ],
            Timings: [new EngineeringCalculationModuleTimingDto("ThermalTopology", 10)],
            CalculationTrace: null,
            CalculationTraceSummary: null,
            EngineeringReport: null,
            ReportPreview: null,
            ReportJson: "{\"report\":\"json\"}",
            ReportMarkdown: "# report",
            Metadata: new Dictionary<string, string>());
    }

    private sealed record Fixture(
        EngineeringCalculationJobService Service,
        RunnerStub Runner,
        InMemoryEngineeringCalculationJobRepository JobRepository);

    private sealed class RunnerStub : IEngineeringCalculationScenarioRunner
    {
        public Func<EngineeringCalculationScenarioRequestDto, EngineeringCalculationScenarioResultDto>? ResultFactory { get; set; }

        public Func<EngineeringCalculationScenarioRequestDto, Exception>? ExceptionFactory { get; set; }

        public int InvocationCount { get; private set; }

        public Task<EngineeringCalculationScenarioResultDto> RunAsync(
            EngineeringCalculationScenarioRequestDto request,
            CancellationToken cancellationToken)
        {
            InvocationCount++;

            if (ExceptionFactory is not null)
            {
                throw ExceptionFactory(request);
            }

            if (ResultFactory is null)
            {
                throw new InvalidOperationException("RunnerStub requires ResultFactory for this test.");
            }

            return Task.FromResult(ResultFactory(request));
        }
    }
}
