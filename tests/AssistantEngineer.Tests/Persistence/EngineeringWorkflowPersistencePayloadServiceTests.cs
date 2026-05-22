using AssistantEngineer.Modules.EngineeringWorkflow.Application.Contracts.EngineeringWorkflow;
using AssistantEngineer.Api.Services.Calculations.Persistence;

namespace AssistantEngineer.Tests.Persistence;

public class EngineeringWorkflowPersistencePayloadServiceTests
{
    [Fact]
    public void ApplyPayloadLimit_UnderLimit_ReturnsUnchangedContent()
    {
        var service = CreateService(maxBytes: 256);
        var content = "{\"value\":\"ok\"}";

        var payload = service.ApplyPayloadLimit("payload", content, 256, "application/json");

        Assert.False(payload.WasTruncated);
        Assert.Equal(content, payload.Content);
        Assert.Equal(payload.OriginalBytes, payload.StoredBytes);
    }

    [Fact]
    public void ApplyPayloadLimit_OverLimitJson_ProducesDeterministicEnvelope()
    {
        var service = CreateService(maxBytes: 96);
        var content = "{\"value\":\"" + new string('x', 4_000) + "\"}";

        var payloadA = service.ApplyPayloadLimit("payload", content, 96, "application/json");
        var payloadB = service.ApplyPayloadLimit("payload", content, 96, "application/json");

        Assert.True(payloadA.WasTruncated);
        Assert.Equal(payloadA.Content, payloadB.Content);
        Assert.Contains("[TRUNCATED_BY_ASSISTANT_ENGINEER_PAYLOAD_LIMIT]", payloadA.Content, StringComparison.Ordinal);
        Assert.True(payloadA.StoredBytes <= 96);
    }

    [Fact]
    public void BuildScenarioRecord_AddsTruncationDiagnostics_WhenPayloadsExceedLimits()
    {
        var service = CreateService(maxBytes: 512);
        var state = CreateState(projectId: 300, buildingId: 3000);
        var request = CreateScenarioRequest("scenario-300", state);
        var result = CreateScenarioResult("scenario-300");

        var record = service.BuildScenarioRecord(
            request,
            result,
            diagnostics: result.ValidationDiagnostics,
            createdAtUtc: DateTimeOffset.UtcNow,
            startedAtUtc: null,
            completedAtUtc: DateTimeOffset.UtcNow);

        Assert.Contains(record.Diagnostics, item => item.Code == "WORKFLOW_REQUEST_JSON_TRUNCATED");
        Assert.Contains(record.Diagnostics, item => item.Code == "WORKFLOW_RESULT_SUMMARY_JSON_TRUNCATED");
        Assert.Contains(record.Diagnostics, item => item.Code == "WORKFLOW_DIAGNOSTICS_JSON_TRUNCATED");
        Assert.Contains("\"truncated\":true", record.Record.RequestJson, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"truncated\":true", record.Record.ResultSummaryJson, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"truncated\":true", record.Record.DiagnosticsJson, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SerializeStatePayload_CompactsOversizedStateDeterministically()
    {
        var service = CreateService(maxBytes: 12_000);
        var state = CreateState(projectId: 301, buildingId: 3001, zoneCount: 100, boundaryCount: 100);
        var normalizedDiagnostics = service.SortAndDistinctDiagnostics(state.Diagnostics);

        var payload = service.SerializeStatePayload(state, normalizedDiagnostics);
        var persisted = service.DeserializeWorkflowState(payload.Content);

        Assert.True(payload.WasTruncated);
        Assert.Contains(payload.Diagnostics, item => item.Code == "WORKFLOW_STATE_JSON_TRUNCATED");
        Assert.NotNull(persisted);
        Assert.Empty(persisted.Zones);
        Assert.Empty(persisted.Boundaries);
        Assert.Equal("true", persisted.Metadata["payloadStateJsonTruncated"]);
        Assert.Contains(persisted.Diagnostics, item => item.Code == "WORKFLOW_STATE_JSON_TRUNCATED");
    }

    private static EngineeringWorkflowPersistencePayloadService CreateService(int maxBytes)
    {
        return new EngineeringWorkflowPersistencePayloadService(new EngineeringWorkflowPayloadLimitsOptions
        {
            Enabled = true,
            RequestJsonMaxBytes = maxBytes,
            StateJsonMaxBytes = maxBytes,
            ResultSummaryJsonMaxBytes = maxBytes,
            DiagnosticsJsonMaxBytes = maxBytes,
            ArtifactContentMaxBytes = maxBytes,
            TruncationMarker = "[TRUNCATED_BY_ASSISTANT_ENGINEER_PAYLOAD_LIMIT]"
        });
    }

    private static EngineeringWorkflowStateDto CreateState(
        int projectId,
        int buildingId,
        int zoneCount = 2,
        int boundaryCount = 2)
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
            Metadata: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["mode"] = "api",
                ["oversized"] = new string('x', 2_000)
            });
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
            Metadata: new Dictionary<string, string>
            {
                ["mode"] = "api",
                ["oversized"] = new string('y', 2_000)
            });
    }
}
