using AssistantEngineer.Modules.EngineeringWorkflow.Application.Contracts.EngineeringWorkflow;
using AssistantEngineer.Api.Services.Calculations;

namespace AssistantEngineer.Tests.Calculations;

public class EngineeringCalculationScenarioStepTests
{
    [Fact]
    public void WeatherSolarStepSkipsWhenWeatherReadinessIsUnavailable()
    {
        var step = new EngineeringCalculationWeatherSolarScenarioStep();
        var request = CreateRequest(weatherStatus: "Unavailable", openingCount: 0);

        var result = step.Execute(request);

        Assert.Equal(EngineeringCalculationModuleExecutionStatus.Skipped, result.Status);
        Assert.Contains(result.Warnings, warning => warning.Contains("Weather and solar readiness", StringComparison.OrdinalIgnoreCase));
        Assert.Empty(result.Values);
    }

    [Fact]
    public void WeatherSolarStepReturnsWorkflowReadinessValuesWhenAvailable()
    {
        var step = new EngineeringCalculationWeatherSolarScenarioStep();
        var request = CreateRequest(weatherStatus: "Available", openingCount: 0);

        var result = step.Execute(request);

        Assert.Equal(EngineeringCalculationModuleExecutionStatus.Executed, result.Status);
        Assert.Equal("WorkflowState.WeatherSolarSettings", result.SourceServiceName);
        Assert.Contains(result.Values, value => value.Key == "weather_status" && Equals(value.Value, "Available"));
        Assert.Contains(result.Values, value => value.Key == "timezone_summary");
        Assert.Contains(result.Values, value => value.Key == "solar_readiness");
    }

    [Fact]
    public void VentilationStepSkipsWhenNoOpeningsAreConfigured()
    {
        var step = new EngineeringCalculationVentilationScenarioStep();
        var request = CreateRequest(weatherStatus: "Available", openingCount: 0);

        var result = step.Execute(request);

        Assert.Equal(EngineeringCalculationModuleExecutionStatus.Skipped, result.Status);
        Assert.Contains(result.Warnings, warning => warning.Contains("No natural ventilation openings", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void VentilationStepSkipsWithStructuredInputDiagnosticWhenOpeningsExistButHourlyPayloadIsMissing()
    {
        var step = new EngineeringCalculationVentilationScenarioStep();
        var request = CreateRequest(weatherStatus: "Available", openingCount: 2);

        var result = step.Execute(request);

        Assert.Equal(EngineeringCalculationModuleExecutionStatus.Skipped, result.Status);
        Assert.Contains(result.Warnings, warning => warning.Contains("Structured natural ventilation hourly input", StringComparison.OrdinalIgnoreCase));
    }

    private static EngineeringCalculationScenarioRequestDto CreateRequest(string weatherStatus, int openingCount) =>
        new(
            ScenarioId: "scenario-step-test",
            ProjectId: 1,
            BuildingId: 10,
            ScenarioKind: EngineeringCalculationScenarioKind.FullEngineeringCore,
            ExecutionMode: EngineeringCalculationExecutionMode.ExecuteAvailableModules,
            State: CreateState(weatherStatus, openingCount));

    private static EngineeringWorkflowStateDto CreateState(string weatherStatus, int openingCount) =>
        new(
            ProjectId: 1,
            ProjectName: "Test project",
            BuildingId: 10,
            CurrentStep: "Review",
            Steps: Array.Empty<EngineeringWorkflowStepDto>(),
            AvailableModules: Array.Empty<string>(),
            BuildingMetadata: new EngineeringWorkflowBuildingMetadataDto(
                BuildingName: "Test building",
                LocationText: "Test city",
                FloorAreaM2: 100,
                VolumeM3: 300,
                NumberOfZones: 1,
                Notes: null),
            Zones: Array.Empty<EngineeringWorkflowZoneDto>(),
            Boundaries: Array.Empty<EngineeringWorkflowBoundaryDto>(),
            WeatherSolarSettings: new EngineeringWorkflowWeatherSolarSettingsDto(
                WeatherSourceStatus: weatherStatus,
                LocationTimezoneSummary: "UTC+05:00 / longitude ready",
                SolarChainReadinessSummary: "Solar chain ready"),
            VentilationSettings: new EngineeringWorkflowVentilationSettingsDto(
                OpeningCount: openingCount,
                ControlModeSummary: "Foundation mode",
                AirflowSummary: "Workflow summary",
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