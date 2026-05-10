using AssistantEngineer.Api.Contracts.Calculations;
using AssistantEngineer.Api.Services.Calculations.Persistence;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Tests.Persistence;

public class EngineeringWorkflowPayloadLimitsTests
{
    [Fact]
    public async Task PersistenceService_CompactsOversizedWorkflowState_AndAddsWarningDiagnostic()
    {
        var service = CreateService(new EngineeringWorkflowPayloadLimitsOptions
        {
            Enabled = true,
            RequestJsonMaxBytes = 32_768,
            StateJsonMaxBytes = 4_096,
            ResultSummaryJsonMaxBytes = 32_768,
            DiagnosticsJsonMaxBytes = 32_768,
            ArtifactContentMaxBytes = 131_072
        });

        var state = CreateState(projectId: 201, buildingId: 2001, zoneCount: 120, boundaryCount: 120);
        await service.SaveWorkflowStateAsync(state, state.Diagnostics, CancellationToken.None);

        var persisted = await service.GetLatestWorkflowStateAsync(201, 2001, CancellationToken.None);

        Assert.NotNull(persisted);
        Assert.Contains(
            persisted.Diagnostics,
            diagnostic => string.Equals(diagnostic.Code, "WORKFLOW_STATE_JSON_TRUNCATED", StringComparison.Ordinal));
        Assert.Empty(persisted.Zones);
        Assert.Empty(persisted.Boundaries);
        Assert.Equal("true", persisted.Metadata["payloadStateJsonTruncated"]);
    }

    [Fact]
    public async Task PersistenceService_TruncatesOversizedScenarioAndArtifactPayloads_Deterministically()
    {
        const int artifactMaxBytes = 1_024;
        var service = CreateService(new EngineeringWorkflowPayloadLimitsOptions
        {
            Enabled = true,
            RequestJsonMaxBytes = 512,
            StateJsonMaxBytes = 65_536,
            ResultSummaryJsonMaxBytes = 512,
            DiagnosticsJsonMaxBytes = 65_536,
            ArtifactContentMaxBytes = artifactMaxBytes
        });

        var state = CreateState(projectId: 202, buildingId: 2002, zoneCount: 2, boundaryCount: 2) with
        {
            Metadata = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["oversized"] = new string('x', 6_000)
            }
        };

        await service.SaveWorkflowStateAsync(state, state.Diagnostics, CancellationToken.None);

        var scenarioId = "scenario-payload-limit-202";
        var request = CreateScenarioRequest(scenarioId, state);
        var result = CreateScenarioResult(scenarioId) with
        {
            ReportJson = "{\"report\":\"" + new string('r', 8_000) + "\"}"
        };

        await service.SaveRunScenarioAsync(request, result, CancellationToken.None);
        var persistedScenario = await service.GetScenarioAsync(scenarioId, CancellationToken.None);
        var artifacts = await service.ListScenarioArtifactsAsync(scenarioId, CancellationToken.None);

        Assert.NotNull(persistedScenario);
        Assert.Contains("\"truncated\":true", persistedScenario.RequestJson, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(persistedScenario.ResultSummaryJson);
        Assert.NotNull(persistedScenario.DiagnosticsJson);
        Assert.Contains("\"truncated\":true", persistedScenario.ResultSummaryJson, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(
            "WORKFLOW_",
            persistedScenario.DiagnosticsJson,
            StringComparison.OrdinalIgnoreCase);
        Assert.Contains(
            "TRUNCATED",
            persistedScenario.DiagnosticsJson,
            StringComparison.OrdinalIgnoreCase);

        var reportArtifact = Assert.Single(artifacts, item => item.ArtifactKind == EngineeringCalculationArtifactKind.ReportJson);
        Assert.True(reportArtifact.SizeBytes <= artifactMaxBytes);
        Assert.Contains("[TRUNCATED_BY_ASSISTANT_ENGINEER_PAYLOAD_LIMIT]", reportArtifact.Content, StringComparison.Ordinal);
    }

    private static EngineeringWorkflowPersistenceService CreateService(EngineeringWorkflowPayloadLimitsOptions limits)
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
                Provider = EngineeringWorkflowPersistenceProvider.InMemory,
                PayloadLimits = limits
            }));
    }

    private static EngineeringWorkflowStateDto CreateState(
        int projectId,
        int buildingId,
        int zoneCount,
        int boundaryCount)
    {
        var zones = Enumerable.Range(1, zoneCount)
            .Select(index => new EngineeringWorkflowZoneDto(
                ZoneId: $"zone-{index:D4}",
                Name: $"Zone {index:D4}",
                ZoneKind: "Conditioned",
                FloorAreaM2: 10 + index,
                AirVolumeM3: 25 + index,
                Status: "valid"))
            .ToArray();

        var boundaries = Enumerable.Range(1, boundaryCount)
            .Select(index => new EngineeringWorkflowBoundaryDto(
                BoundaryId: $"boundary-{index:D4}",
                ZoneOrRoomName: $"Zone {index:D4}",
                ExposureKind: "External",
                AreaM2: 5 + index,
                UValue: 0.4,
                AdjacentZoneReference: null,
                Indicator: "exterior",
                ValidationStatus: "valid"))
            .ToArray();

        return new EngineeringWorkflowStateDto(
            ProjectId: projectId,
            ProjectName: $"Project {projectId}",
            BuildingId: buildingId,
            CurrentStep: "Review",
            Steps: [new EngineeringWorkflowStepDto("Review", "valid", true)],
            AvailableModules: ["ThermalTopology", "SystemEnergy"],
            BuildingMetadata: new EngineeringWorkflowBuildingMetadataDto(
                BuildingName: "Payload test building",
                LocationText: "Tashkent",
                FloorAreaM2: 120.0,
                VolumeM3: 320.0,
                NumberOfZones: zoneCount,
                Notes: new string('n', 2_000)),
            Zones: zones,
            Boundaries: boundaries,
            WeatherSolarSettings: new EngineeringWorkflowWeatherSolarSettingsDto("Ready", "UTC+05", "Ready"),
            VentilationSettings: new EngineeringWorkflowVentilationSettingsDto(1, "Auto", "Configured", ["none"]),
            GroundSettings: new EngineeringWorkflowGroundSettingsDto(1, "Constant", "valid"),
            DomesticHotWaterSettings: new EngineeringWorkflowDomesticHotWaterSettingsDto("PerPerson", "1200", "200", "NoDoubleCounting"),
            SystemEnergySettings: new EngineeringWorkflowSystemEnergySettingsDto("Heating,DHW", "Electricity", "Ready"),
            Diagnostics: [new EngineeringWorkflowDiagnosticDto("warning", "WF_WARN", "Workflow warning", "Validation")],
            Assumptions: ["Foundation state."],
            Links: ["/api/v1/engineering-workflow/validate"],
            CalculationTraceSummary: null,
            ReportSummary: null,
            Metadata: new Dictionary<string, string>(StringComparer.Ordinal) { ["mode"] = "api" });
    }

    private static EngineeringCalculationScenarioRequestDto CreateScenarioRequest(
        string scenarioId,
        EngineeringWorkflowStateDto state)
    {
        return new EngineeringCalculationScenarioRequestDto(
            ScenarioId: scenarioId,
            ProjectId: state.ProjectId,
            BuildingId: state.BuildingId,
            ScenarioKind: EngineeringCalculationScenarioKind.FullEngineeringCore,
            ExecutionMode: EngineeringCalculationExecutionMode.ExecuteAvailableModules,
            State: state,
            RequestedModules: state.AvailableModules,
            DetailLevel: "Summary",
            IncludeTrace: false,
            IncludeReport: true,
            ReportFormats: ["Json"],
            DeterministicTimestampUtc: null,
            DiagnosticsMode: "Deterministic");
    }

    private static EngineeringCalculationScenarioResultDto CreateScenarioResult(string scenarioId)
    {
        return new EngineeringCalculationScenarioResultDto(
            ScenarioId: scenarioId,
            Status: EngineeringCalculationExecutionStatus.CompletedWithWarnings,
            Executed: true,
            ExecutedModules: ["ThermalTopology"],
            SkippedModules: [],
            UnavailableModules: [],
            ValidationDiagnostics: [new EngineeringWorkflowDiagnosticDto("warning", "WF_WARN", "Warning", "Validation")],
            Assumptions: ["Scenario result foundation fixture."],
            Warnings: ["Warning fixture."],
            ModuleSummaries: new EngineeringCalculationModuleSummariesDto(
                TopologySummary: new string('t', 4_000),
                VentilationSummary: "Configured",
                GroundSummary: "Configured",
                HeatingCoolingSummary: "Skipped",
                DomesticHotWaterSummary: "Skipped",
                SystemEnergySummary: "Executed"),
            ModuleResults: [],
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
