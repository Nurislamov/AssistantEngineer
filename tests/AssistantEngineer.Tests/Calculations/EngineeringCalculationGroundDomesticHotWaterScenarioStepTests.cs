using AssistantEngineer.Modules.EngineeringWorkflow.Application.Contracts.EngineeringWorkflow;
using AssistantEngineer.Api.Services.Calculations;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.DomesticHotWater;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Tests.Calculations;

public class EngineeringCalculationGroundDomesticHotWaterScenarioStepTests
{
    [Fact]
    public void GroundStepSkipsWhenNoGroundBoundariesAreConfigured()
    {
        var step = new EngineeringCalculationGroundScenarioStep();
        var request = CreateRequest(groundBoundaryCount: 0, dhwUsefulAnnualKWh: null);

        var result = step.Execute(request);

        Assert.Equal(EngineeringCalculationModuleExecutionStatus.Skipped, result.Status);
        Assert.Contains(result.Warnings, warning => warning.Contains("No ground boundaries", StringComparison.OrdinalIgnoreCase));
        Assert.Empty(result.Values);
    }

    [Fact]
    public void GroundStepSkipsWithStructuredInputDiagnosticWhenGroundBoundariesExistButPayloadIsMissing()
    {
        var step = new EngineeringCalculationGroundScenarioStep();
        var request = CreateRequest(groundBoundaryCount: 2, dhwUsefulAnnualKWh: null);

        var result = step.Execute(request);

        Assert.Equal(EngineeringCalculationModuleExecutionStatus.Skipped, result.Status);
        Assert.Contains(result.Warnings, warning => warning.Contains("Structured ground boundary geometry", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void DomesticHotWaterStepSkipsWhenAnnualUsefulDemandMetadataIsMissing()
    {
        var calculator = new StubDomesticHotWaterSystemLoadCalculator();
        var step = new EngineeringCalculationDomesticHotWaterScenarioStep(calculator);
        var request = CreateRequest(groundBoundaryCount: 0, dhwUsefulAnnualKWh: null);

        var result = step.Execute(request);

        Assert.Equal(EngineeringCalculationModuleExecutionStatus.Skipped, result.Execution.Status);
        Assert.Null(result.Summary);
        Assert.Null(calculator.LastRequest);
        Assert.Contains(result.Execution.Warnings, warning => warning.Contains("dhw.useful_annual_kwh", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void DomesticHotWaterStepExecutesCalculatorWhenAnnualUsefulDemandMetadataIsAvailable()
    {
        var calculator = new StubDomesticHotWaterSystemLoadCalculator();
        var step = new EngineeringCalculationDomesticHotWaterScenarioStep(calculator);
        var request = CreateRequest(groundBoundaryCount: 0, dhwUsefulAnnualKWh: "876.0");

        var result = step.Execute(request);

        Assert.Equal(EngineeringCalculationModuleExecutionStatus.Executed, result.Execution.Status);
        Assert.NotNull(result.Summary);
        Assert.NotNull(calculator.LastRequest);
        Assert.Equal(8760, calculator.LastRequest!.UsefulDemandProfileKWh.Count);
        Assert.Equal(876.0, calculator.LastRequest.UsefulDemandProfileKWh.Sum(), precision: 6);
        Assert.Contains(result.Execution.Values, value => value.Key == "dhw_annual_useful_kwh" && Math.Abs(Convert.ToDouble(value.Value) - 876.0) < 1e-6);
        Assert.Contains(result.Assumptions, item => item.Contains("stub assumption", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Warnings, item => item.Contains("stub warning", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Diagnostics, item => item.Code == "DHW_STUB_WARNING" && item.SourceModule == "DhwFoundation");
    }

    private static EngineeringCalculationScenarioRequestDto CreateRequest(int groundBoundaryCount, string? dhwUsefulAnnualKWh) =>
        new(
            ScenarioId: "scenario-ground-dhw-step-test",
            ProjectId: 1,
            BuildingId: 10,
            ScenarioKind: EngineeringCalculationScenarioKind.FullEngineeringCore,
            ExecutionMode: EngineeringCalculationExecutionMode.ExecuteAvailableModules,
            State: CreateState(groundBoundaryCount, dhwUsefulAnnualKWh));

    private static EngineeringWorkflowStateDto CreateState(int groundBoundaryCount, string? dhwUsefulAnnualKWh)
    {
        var metadata = new Dictionary<string, string>();
        if (!string.IsNullOrWhiteSpace(dhwUsefulAnnualKWh))
        {
            metadata["dhw.useful_annual_kwh"] = dhwUsefulAnnualKWh;
        }

        return new EngineeringWorkflowStateDto(
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
                WeatherSourceStatus: "Available",
                LocationTimezoneSummary: "UTC+05:00 / longitude ready",
                SolarChainReadinessSummary: "Solar chain ready"),
            VentilationSettings: new EngineeringWorkflowVentilationSettingsDto(
                OpeningCount: 0,
                ControlModeSummary: "Foundation mode",
                AirflowSummary: "Workflow summary",
                Warnings: Array.Empty<string>()),
            GroundSettings: new EngineeringWorkflowGroundSettingsDto(
                GroundBoundaryCount: groundBoundaryCount,
                GroundProfileMode: "Foundation mode",
                SummaryStatus: "Workflow summary"),
            DomesticHotWaterSettings: new EngineeringWorkflowDomesticHotWaterSettingsDto(
                DemandBasis: "metadata",
                UsefulDemandSummary: "Workflow summary",
                LossesSummary: "Workflow summary",
                OwnershipPolicy: "DhwOwnLosses"),
            SystemEnergySettings: new EngineeringWorkflowSystemEnergySettingsDto(
                UsesSummary: "n/a",
                CarriersSummary: "n/a",
                FinalPrimaryCarbonSummary: "n/a"),
            Diagnostics: Array.Empty<EngineeringWorkflowDiagnosticDto>(),
            Assumptions: Array.Empty<string>(),
            Links: Array.Empty<string>(),
            CalculationTraceSummary: null,
            ReportSummary: null,
            Metadata: metadata);
    }

    private sealed class StubDomesticHotWaterSystemLoadCalculator : IDomesticHotWaterSystemLoadCalculator
    {
        public DomesticHotWaterSystemLoadRequest? LastRequest { get; private set; }

        public DomesticHotWaterSystemLoadResult Calculate(DomesticHotWaterSystemLossInput input) =>
            throw new NotSupportedException("The scenario step only uses the foundation request overload.");

        public DomesticHotWaterSystemLoadFoundationResult Calculate(DomesticHotWaterSystemLoadRequest request)
        {
            LastRequest = request;
            var usefulAnnual = request.UsefulDemandProfileKWh.Sum();
            var systemAnnual = usefulAnnual + 12.0;
            var systemProfile = request.UsefulDemandProfileKWh
                .Select(value => value + 12.0 / 8760.0)
                .ToArray();

            return new DomesticHotWaterSystemLoadFoundationResult(
                UsefulEnergyProfileKWh: request.UsefulDemandProfileKWh,
                StorageLossesProfileKWh: FlatProfile(4.0),
                DistributionLossesProfileKWh: FlatProfile(4.0),
                CirculationLossesProfileKWh: FlatProfile(4.0),
                RecoveredLossesProfileKWh: FlatProfile(0.0),
                AuxiliaryEnergyProfileKWh: FlatProfile(0.0),
                SystemLoadProfileKWh: systemProfile,
                MonthlySystemLoadKWh: Enumerable.Repeat(systemAnnual / 12.0, 12).ToArray(),
                AnnualSummary: new DomesticHotWaterSystemLoadAnnualSummary(
                    UsefulEnergyKWh: usefulAnnual,
                    StorageLossesKWh: 4.0,
                    DistributionLossesKWh: 4.0,
                    CirculationLossesKWh: 4.0,
                    RecoveredLossesKWh: 0.0,
                    AuxiliaryEnergyKWh: 0.0,
                    SystemLoadKWh: systemAnnual),
                Assumptions: ["stub assumption"],
                Warnings: ["stub warning"],
                Diagnostics:
                [
                    new StandardCalculationDiagnostic(
                        CalculationDiagnosticSeverity.Warning,
                        "DHW_STUB_WARNING",
                        "Stub DHW diagnostic.",
                        Context: "dhw.useful_annual_kwh")
                ]);
        }

        private static IReadOnlyList<double> FlatProfile(double annualKWh) =>
            Enumerable.Repeat(annualKWh / 8760.0, 8760).ToArray();
    }
}