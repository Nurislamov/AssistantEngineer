using AssistantEngineer.Modules.EngineeringWorkflow.Application.Contracts.EngineeringWorkflow;
using AssistantEngineer.Api.Services.Calculations;

namespace AssistantEngineer.Tests.Calculations;

public class EngineeringCalculationScenarioRequestValidatorTests
{
    [Fact]
    public void ValidateReturnsBlockingDiagnosticsForInvalidFullExecutionRequest()
    {
        var validator = new EngineeringCalculationScenarioRequestValidator();
        var request = new EngineeringCalculationScenarioRequestDto(
            ScenarioId: string.Empty,
            ProjectId: null,
            BuildingId: null,
            ScenarioKind: EngineeringCalculationScenarioKind.FullEngineeringCore,
            ExecutionMode: EngineeringCalculationExecutionMode.ExecuteFullRequired,
            State: CreateState(projectId: 0, buildingId: null));

        var diagnostics = validator.Validate(request);

        Assert.Contains(diagnostics, item => item.Code == "SCENARIO_ID_MISSING" && item.Severity == "error");
        Assert.Contains(diagnostics, item => item.Code == "SCENARIO_PROJECT_ID_INVALID" && item.Severity == "error");
        Assert.Contains(diagnostics, item => item.Code == "SCENARIO_BUILDING_ID_MISSING" && item.Severity == "warning");
        Assert.Contains(diagnostics, item => item.Code == "SCENARIO_ZONES_REQUIRED" && item.Severity == "error");
        Assert.Contains(diagnostics, item => item.Code == "SCENARIO_BOUNDARIES_REQUIRED" && item.Severity == "error");
        Assert.True(validator.HasErrors(diagnostics));
    }

    [Fact]
    public void SortAndDistinctDropsBlankMessagesAndDeduplicatesDiagnostics()
    {
        var validator = new EngineeringCalculationScenarioRequestValidator();
        var diagnostics = new[]
        {
            new EngineeringWorkflowDiagnosticDto("info", "INFO", "Duplicate", "Review", TargetField: "field"),
            new EngineeringWorkflowDiagnosticDto("info", "INFO", "Duplicate", "Review", TargetField: "field"),
            new EngineeringWorkflowDiagnosticDto("warning", "WARN", "Warning", "Review"),
            new EngineeringWorkflowDiagnosticDto("error", "ERR", "Error", "Review"),
            new EngineeringWorkflowDiagnosticDto("info", "BLANK", string.Empty, "Review")
        };

        var sorted = validator.SortAndDistinct(diagnostics);

        Assert.Equal(3, sorted.Count);
        Assert.Equal("ERR", sorted[0].Code);
        Assert.Equal("WARN", sorted[1].Code);
        Assert.Equal("INFO", sorted[2].Code);
    }

    private static EngineeringWorkflowStateDto CreateState(int projectId, int? buildingId) =>
        new(
            ProjectId: projectId,
            ProjectName: "Test project",
            BuildingId: buildingId,
            CurrentStep: "Review",
            Steps: Array.Empty<EngineeringWorkflowStepDto>(),
            AvailableModules: Array.Empty<string>(),
            BuildingMetadata: new EngineeringWorkflowBuildingMetadataDto(
                BuildingName: null,
                LocationText: null,
                FloorAreaM2: null,
                VolumeM3: null,
                NumberOfZones: null,
                Notes: null),
            Zones: Array.Empty<EngineeringWorkflowZoneDto>(),
            Boundaries: Array.Empty<EngineeringWorkflowBoundaryDto>(),
            WeatherSolarSettings: new EngineeringWorkflowWeatherSolarSettingsDto(
                WeatherSourceStatus: "Unavailable",
                LocationTimezoneSummary: "n/a",
                SolarChainReadinessSummary: "n/a"),
            VentilationSettings: new EngineeringWorkflowVentilationSettingsDto(
                OpeningCount: 0,
                ControlModeSummary: "n/a",
                AirflowSummary: "n/a",
                Warnings: Array.Empty<string>()),
            GroundSettings: new EngineeringWorkflowGroundSettingsDto(
                GroundBoundaryCount: 0,
                GroundProfileMode: "n/a",
                SummaryStatus: "n/a"),
            DomesticHotWaterSettings: new EngineeringWorkflowDomesticHotWaterSettingsDto(
                DemandBasis: "n/a",
                UsefulDemandSummary: "n/a",
                LossesSummary: "n/a",
                OwnershipPolicy: "n/a"),
            SystemEnergySettings: new EngineeringWorkflowSystemEnergySettingsDto(
                UsesSummary: "n/a",
                CarriersSummary: "n/a",
                FinalPrimaryCarbonSummary: "n/a"),
            Diagnostics: Array.Empty<EngineeringWorkflowDiagnosticDto>(),
            Assumptions: Array.Empty<string>(),
            Links: Array.Empty<string>(),
            CalculationTraceSummary: null,
            ReportSummary: null,
            Metadata: new Dictionary<string, string>());
}