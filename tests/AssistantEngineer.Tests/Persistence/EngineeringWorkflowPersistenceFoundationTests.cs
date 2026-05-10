using AssistantEngineer.Api.Contracts.Calculations;
using AssistantEngineer.Api.Services.Calculations.Persistence;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Tests.Persistence;

public class EngineeringWorkflowPersistenceFoundationTests
{
    [Fact]
    public async Task InMemoryRepositoriesSupportCreateUpdateAndDeterministicReads()
    {
        var store = new EngineeringWorkflowMemoryStore();
        var projectRepository = new InMemoryEngineeringProjectRepository(store);
        var workflowRepository = new InMemoryEngineeringWorkflowStateRepository(store);
        var scenarioRepository = new InMemoryEngineeringCalculationScenarioRepository(store);
        var artifactRepository = new InMemoryEngineeringCalculationArtifactRepository(store);
        var historyRepository = new InMemoryEngineeringScenarioHistoryRepository(store);

        var now = DateTimeOffset.UtcNow;
        await projectRepository.UpsertAsync(
            new EngineeringProjectRecordDto(
                ProjectId: 10,
                ProjectName: "Project 10",
                Description: null,
                CreatedAtUtc: now,
                UpdatedAtUtc: now,
                Status: EngineeringProjectRecordStatus.Active,
                MetadataJson: new Dictionary<string, string>()),
            CancellationToken.None);

        var state = new EngineeringWorkflowStateRecordDto(
            WorkflowStateId: "wf-10-0001",
            ProjectId: 10,
            BuildingId: 100,
            Version: 1,
            CurrentStep: "Validation",
            WorkflowStateJson: "{\"projectId\":10}",
            ValidationDiagnosticsJson: "[]",
            CreatedAtUtc: now,
            UpdatedAtUtc: now);
        await workflowRepository.SaveAsync(state, CancellationToken.None);

        var scenario = new EngineeringCalculationScenarioRecordDto(
            ScenarioId: "scenario-10",
            ProjectId: 10,
            BuildingId: 100,
            ScenarioKind: EngineeringCalculationScenarioKind.FullEngineeringCore,
            ExecutionMode: EngineeringCalculationExecutionMode.ExecuteAvailableModules,
            Status: EngineeringCalculationExecutionStatus.PartiallyExecuted,
            RequestJson: "{}",
            ResultSummaryJson: "{\"status\":\"PartiallyExecuted\"}",
            CreatedAtUtc: now,
            StartedAtUtc: now,
            CompletedAtUtc: now.AddSeconds(1),
            DurationMilliseconds: 1000,
            DiagnosticsJson: "[]");
        await scenarioRepository.CreateAsync(scenario, CancellationToken.None);

        var artifact = new EngineeringCalculationArtifactRecordDto(
            ArtifactId: "scenario-10:ScenarioResultJson",
            ScenarioId: "scenario-10",
            ArtifactKind: EngineeringCalculationArtifactKind.ScenarioResultJson,
            ContentType: "application/json",
            Content: "{\"scenarioId\":\"scenario-10\"}",
            CreatedAtUtc: now,
            SizeBytes: 27,
            ChecksumSha256: "checksum");
        await artifactRepository.SaveAsync(artifact, CancellationToken.None);

        await historyRepository.AppendAsync(
            new EngineeringScenarioHistoryEntryDto(
                EventId: "scenario-10:Created:1",
                ScenarioId: "scenario-10",
                ProjectId: 10,
                EventKind: EngineeringScenarioHistoryEventKind.Created,
                Message: "Created.",
                DiagnosticsJson: "[]",
                CreatedAtUtc: now),
            CancellationToken.None);

        var latestState = await workflowRepository.GetLatestByProjectIdAsync(10, CancellationToken.None);
        Assert.NotNull(latestState);
        Assert.Equal("wf-10-0001", latestState.WorkflowStateId);

        var latestScenario = await scenarioRepository.GetLatestByProjectIdAsync(10, CancellationToken.None);
        Assert.NotNull(latestScenario);
        Assert.Equal("scenario-10", latestScenario.ScenarioId);

        var scenarioArtifacts = await artifactRepository.ListByScenarioIdAsync("scenario-10", CancellationToken.None);
        Assert.Single(scenarioArtifacts);
        Assert.Equal(EngineeringCalculationArtifactKind.ScenarioResultJson, scenarioArtifacts[0].ArtifactKind);

        var projectHistory = await historyRepository.ListByProjectIdAsync(10, CancellationToken.None);
        Assert.Single(projectHistory);
        Assert.Equal(EngineeringScenarioHistoryEventKind.Created, projectHistory[0].EventKind);
    }

    [Fact]
    public async Task PersistenceServiceSavesRunScenarioSummaryAndArtifacts()
    {
        var service = CreateService();
        var state = CreateState(projectId: 11, buildingId: 110);
        var request = CreateScenarioRequest("scenario-run-11", state, EngineeringCalculationExecutionMode.ExecuteAvailableModules);
        var result = CreateScenarioResult("scenario-run-11", EngineeringCalculationExecutionStatus.PartiallyExecuted);

        await service.SaveWorkflowStateAsync(state, state.Diagnostics, CancellationToken.None);
        var persisted = await service.SaveRunScenarioAsync(request, result, CancellationToken.None);

        Assert.Equal("scenario-run-11", persisted.ScenarioId);
        Assert.NotNull(persisted.ResultSummaryJson);
        Assert.DoesNotContain("reportMarkdown", persisted.ResultSummaryJson!, StringComparison.OrdinalIgnoreCase);

        var scenarios = await service.ListProjectScenariosAsync(11, CancellationToken.None);
        Assert.Single(scenarios);

        var artifacts = await service.ListScenarioArtifactsAsync("scenario-run-11", CancellationToken.None);
        Assert.Contains(artifacts, item => item.ArtifactKind == EngineeringCalculationArtifactKind.ScenarioResultJson);
        Assert.Contains(artifacts, item => item.ArtifactKind == EngineeringCalculationArtifactKind.ValidationDiagnostics);

        var scenarioResultArtifact = await service.GetScenarioArtifactAsync(
            "scenario-run-11",
            EngineeringCalculationArtifactKind.ScenarioResultJson,
            CancellationToken.None);
        Assert.NotNull(scenarioResultArtifact);
        Assert.Contains("\"scenarioId\"", scenarioResultArtifact.Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PersistenceServiceReturnsPersistedWorkflowState()
    {
        var service = CreateService();
        var state = CreateState(projectId: 12, buildingId: 120);

        await service.SaveWorkflowStateAsync(state, state.Diagnostics, CancellationToken.None);
        var persisted = await service.GetLatestWorkflowStateAsync(12, 120, CancellationToken.None);

        Assert.NotNull(persisted);
        Assert.Equal(12, persisted.ProjectId);
        Assert.Equal(120, persisted.BuildingId);
        Assert.Equal("in-memory-foundation", persisted.Metadata["persistence"]);
        Assert.Equal("InMemory", persisted.Metadata["persistenceProvider"]);
        Assert.Equal("false", persisted.Metadata["durablePersistenceEnabled"]);
    }

    private static EngineeringWorkflowPersistenceService CreateService()
    {
        var store = new EngineeringWorkflowMemoryStore();
        return new EngineeringWorkflowPersistenceService(
            new InMemoryEngineeringProjectRepository(store),
            new InMemoryEngineeringWorkflowStateRepository(store),
            new InMemoryEngineeringCalculationScenarioRepository(store),
            new InMemoryEngineeringCalculationArtifactRepository(store),
            new InMemoryEngineeringScenarioHistoryRepository(store),
            Options.Create(new EngineeringWorkflowPersistenceOptions
            {
                Provider = EngineeringWorkflowPersistenceProvider.InMemory
            }));
    }

    private static EngineeringWorkflowStateDto CreateState(int projectId, int buildingId)
    {
        return new EngineeringWorkflowStateDto(
            ProjectId: projectId,
            ProjectName: $"Project {projectId}",
            BuildingId: buildingId,
            CurrentStep: "Review",
            Steps: [new EngineeringWorkflowStepDto("Review", "valid", true)],
            AvailableModules: ["ThermalTopology", "SystemEnergy"],
            BuildingMetadata: new EngineeringWorkflowBuildingMetadataDto(
                BuildingName: "Building",
                LocationText: "Location",
                FloorAreaM2: 100,
                VolumeM3: 250,
                NumberOfZones: 1,
                Notes: "Foundation"),
            Zones: [new EngineeringWorkflowZoneDto("zone-1", "Zone 1", "Conditioned", 100, 250, "valid")],
            Boundaries: [new EngineeringWorkflowBoundaryDto("b-1", "Zone 1", "External", 30, 0.45, null, "exterior", "valid")],
            WeatherSolarSettings: new EngineeringWorkflowWeatherSolarSettingsDto("Ready", "UTC+05", "Ready"),
            VentilationSettings: new EngineeringWorkflowVentilationSettingsDto(1, "Auto", "Configured", []),
            GroundSettings: new EngineeringWorkflowGroundSettingsDto(1, "Constant", "valid"),
            DomesticHotWaterSettings: new EngineeringWorkflowDomesticHotWaterSettingsDto("PerPerson", "1200", "200", "NoDoubleCounting"),
            SystemEnergySettings: new EngineeringWorkflowSystemEnergySettingsDto("Heating,DHW", "Electricity", "Ready"),
            Diagnostics: [new EngineeringWorkflowDiagnosticDto("warning", "WF_WARN", "Workflow warning", "Validation")],
            Assumptions: ["Foundation state."],
            Links: ["/api/v1/engineering-workflow/validate"],
            CalculationTraceSummary: null,
            ReportSummary: null,
            Metadata: new Dictionary<string, string> { ["mode"] = "api" });
    }

    private static EngineeringCalculationScenarioRequestDto CreateScenarioRequest(
        string scenarioId,
        EngineeringWorkflowStateDto state,
        EngineeringCalculationExecutionMode mode)
    {
        return new EngineeringCalculationScenarioRequestDto(
            ScenarioId: scenarioId,
            ProjectId: state.ProjectId,
            BuildingId: state.BuildingId,
            ScenarioKind: EngineeringCalculationScenarioKind.FullEngineeringCore,
            ExecutionMode: mode,
            State: state,
            RequestedModules: state.AvailableModules,
            DetailLevel: "Summary",
            IncludeTrace: false,
            IncludeReport: false,
            ReportFormats: ["Json"],
            DeterministicTimestampUtc: null,
            DiagnosticsMode: "Deterministic");
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
            SkippedModules: ["HeatingCooling"],
            UnavailableModules: [],
            ValidationDiagnostics:
            [
                new EngineeringWorkflowDiagnosticDto(
                    Severity: "warning",
                    Code: "WF_SKIP",
                    Message: "Heating/cooling skipped.",
                    SourceStep: "Validation")
            ],
            Assumptions: ["Scenario result foundation fixture."],
            Warnings: ["Skipped module warning."],
            ModuleSummaries: new EngineeringCalculationModuleSummariesDto(
                TopologySummary: "Executed.",
                VentilationSummary: "Skipped.",
                GroundSummary: "Skipped.",
                HeatingCoolingSummary: "Skipped.",
                DomesticHotWaterSummary: "Skipped.",
                SystemEnergySummary: "Skipped."),
            ModuleResults:
            [
                new EngineeringCalculationModuleExecutionResultDto(
                    ModuleKind: "ThermalTopology",
                    Status: EngineeringCalculationModuleExecutionStatus.Executed,
                    SummaryValues:
                    [
                        new EngineeringCalculationModuleValueDto("zones", "Zone count", 1)
                    ],
                    Diagnostics: [],
                    Assumptions: [],
                    Warnings: [],
                    DurationMilliseconds: 12,
                    SourceServiceName: "test")
            ],
            Timings: [new EngineeringCalculationModuleTimingDto("ThermalTopology", 12)],
            CalculationTrace: null,
            CalculationTraceSummary: null,
            EngineeringReport: null,
            ReportPreview: null,
            ReportJson: null,
            ReportMarkdown: null,
            Metadata: new Dictionary<string, string> { ["mode"] = "api" });
    }
}
