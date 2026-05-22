using AssistantEngineer.Modules.EngineeringWorkflow.Application.Contracts.EngineeringWorkflow;
using AssistantEngineer.Api.Services.Calculations;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.SystemEnergy;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;
using AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;

namespace AssistantEngineer.Tests.Calculations;

public class EngineeringCalculationSystemEnergyScenarioStepTests
{
    [Fact]
    public void SystemEnergyStepSkipsWhenNoUsefulLoadsOrDhwHandoffAreAvailable()
    {
        var calculator = new StubSystemEnergyFoundationCalculator();
        var step = new EngineeringCalculationSystemEnergyScenarioStep(calculator);
        var request = CreateRequest(new Dictionary<string, string>());

        var result = step.Execute(request, domesticHotWaterSummary: null);

        Assert.Equal(EngineeringCalculationModuleExecutionStatus.Skipped, result.Execution.Status);
        Assert.Null(result.Summary);
        Assert.Null(calculator.LastRequest);
        Assert.Contains(result.Execution.Warnings, warning => warning.Contains("useful loads are unavailable", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void SystemEnergyStepExecutesCalculatorWhenAnnualLoadMetadataIsAvailable()
    {
        var calculator = new StubSystemEnergyFoundationCalculator();
        var step = new EngineeringCalculationSystemEnergyScenarioStep(calculator);
        var request = CreateRequest(new Dictionary<string, string>
        {
            ["system_energy.heating_annual_kwh"] = "1000.0",
            ["system_energy.cooling_annual_kwh"] = "250.0"
        });

        var result = step.Execute(request, domesticHotWaterSummary: null);

        Assert.Equal(EngineeringCalculationModuleExecutionStatus.Executed, result.Execution.Status);
        Assert.NotNull(result.Summary);
        Assert.NotNull(calculator.LastRequest);
        Assert.Equal("scenario-system-energy-step-test", calculator.LastRequest!.CalculationId);
        Assert.Equal(2, calculator.LastRequest.LoadInputs.Count);
        Assert.All(calculator.LastRequest.LoadInputs, load => Assert.Equal(8760, load.HourlyUsefulEnergyKWh8760.Count));
        Assert.Contains(calculator.LastRequest.LoadInputs, load => load.EndUse == SystemEnergyEndUse.SpaceHeating && Math.Abs(load.AnnualUsefulEnergyKWh!.Value - 1000.0) < 1e-6);
        Assert.Contains(calculator.LastRequest.LoadInputs, load => load.EndUse == SystemEnergyEndUse.SpaceCooling && Math.Abs(load.AnnualUsefulEnergyKWh!.Value - 250.0) < 1e-6);
        Assert.Equal(SystemEnergyProfileShape.Hourly8760, calculator.LastRequest.OutputResolution);
        Assert.False(calculator.LastRequest.StrictFactorMode);
        Assert.Contains(result.Execution.Values, value => value.Key == "system_final_kwh" && Math.Abs(Convert.ToDouble(value.Value) - 1234.0) < 1e-6);
        Assert.Contains(result.Assumptions, item => item.Contains("stub assumption", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Warnings, item => item.Contains("stub warning", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Diagnostics, item => item.Code == "SYS_STUB_WARNING" && item.SourceModule == "SystemEnergyFoundation");
    }

    [Fact]
    public void SystemEnergyStepCanUseDomesticHotWaterFoundationHandoff()
    {
        var calculator = new StubSystemEnergyFoundationCalculator();
        var step = new EngineeringCalculationSystemEnergyScenarioStep(calculator);
        var request = CreateRequest(new Dictionary<string, string>());
        var dhwSummary = CreateDomesticHotWaterSummary(876.0, 888.0);

        var result = step.Execute(request, dhwSummary);

        Assert.Equal(EngineeringCalculationModuleExecutionStatus.Executed, result.Execution.Status);
        Assert.NotNull(calculator.LastRequest);
        var load = Assert.Single(calculator.LastRequest!.LoadInputs);
        Assert.Equal(SystemEnergyEndUse.DomesticHotWater, load.EndUse);
        Assert.Equal("load-dhw-foundation", load.LoadId);
        Assert.Equal(876.0, load.AnnualUsefulEnergyKWh!.Value, precision: 6);
        Assert.NotNull(load.HourlySystemLoadKWh8760);
        Assert.Equal(8760, load.HourlySystemLoadKWh8760!.Count);
    }

    private static EngineeringCalculationScenarioRequestDto CreateRequest(IReadOnlyDictionary<string, string> metadata) =>
        new(
            ScenarioId: "scenario-system-energy-step-test",
            ProjectId: 1,
            BuildingId: 10,
            ScenarioKind: EngineeringCalculationScenarioKind.FullEngineeringCore,
            ExecutionMode: EngineeringCalculationExecutionMode.ExecuteAvailableModules,
            State: CreateState(metadata));

    private static EngineeringWorkflowStateDto CreateState(IReadOnlyDictionary<string, string> metadata) =>
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
                WeatherSourceStatus: "Available",
                LocationTimezoneSummary: "UTC+05:00 / longitude ready",
                SolarChainReadinessSummary: "Solar chain ready"),
            VentilationSettings: new EngineeringWorkflowVentilationSettingsDto(
                OpeningCount: 0,
                ControlModeSummary: "Foundation mode",
                AirflowSummary: "Workflow summary",
                Warnings: Array.Empty<string>()),
            GroundSettings: new EngineeringWorkflowGroundSettingsDto(
                GroundBoundaryCount: 0,
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

    private static DomesticHotWaterSystemLoadFoundationResult CreateDomesticHotWaterSummary(double usefulAnnual, double systemAnnual) =>
        new(
            UsefulEnergyProfileKWh: FlatProfile(usefulAnnual),
            StorageLossesProfileKWh: FlatProfile(4.0),
            DistributionLossesProfileKWh: FlatProfile(4.0),
            CirculationLossesProfileKWh: FlatProfile(4.0),
            RecoveredLossesProfileKWh: FlatProfile(0.0),
            AuxiliaryEnergyProfileKWh: FlatProfile(0.0),
            SystemLoadProfileKWh: FlatProfile(systemAnnual),
            MonthlySystemLoadKWh: Enumerable.Repeat(systemAnnual / 12.0, 12).ToArray(),
            AnnualSummary: new DomesticHotWaterSystemLoadAnnualSummary(
                UsefulEnergyKWh: usefulAnnual,
                StorageLossesKWh: 4.0,
                DistributionLossesKWh: 4.0,
                CirculationLossesKWh: 4.0,
                RecoveredLossesKWh: 0.0,
                AuxiliaryEnergyKWh: 0.0,
                SystemLoadKWh: systemAnnual),
            Assumptions: [],
            Warnings: [],
            Diagnostics: []);

    private static IReadOnlyList<double> FlatProfile(double annualKWh) =>
        Enumerable.Repeat(annualKWh / 8760.0, 8760).ToArray();

    private sealed class StubSystemEnergyFoundationCalculator : ISystemEnergyFoundationCalculator
    {
        public SystemEnergyCalculationRequest? LastRequest { get; private set; }

        public SystemEnergyCalculationResult Calculate(SystemEnergyCalculationRequest request)
        {
            LastRequest = request;

            return new SystemEnergyCalculationResult(
                UsefulEnergyByUseKWh: new Dictionary<SystemEnergyUseKind, IReadOnlyList<double>>(),
                SystemLoadByUseKWh: new Dictionary<SystemEnergyUseKind, IReadOnlyList<double>>(),
                EmissionLossesByUseKWh: new Dictionary<SystemEnergyUseKind, IReadOnlyList<double>>(),
                DistributionLossesByUseKWh: new Dictionary<SystemEnergyUseKind, IReadOnlyList<double>>(),
                StorageLossesByUseKWh: new Dictionary<SystemEnergyUseKind, IReadOnlyList<double>>(),
                GenerationLossesByUseKWh: new Dictionary<SystemEnergyUseKind, IReadOnlyList<double>>(),
                RecoveredLossesByUseKWh: new Dictionary<SystemEnergyUseKind, IReadOnlyList<double>>(),
                AuxiliaryEnergyByUseKWh: new Dictionary<SystemEnergyUseKind, IReadOnlyList<double>>(),
                FinalEnergyByCarrierKWh: new Dictionary<SystemEnergyCarrierKind, IReadOnlyList<double>>(),
                PrimaryEnergyByCarrierKWh: new Dictionary<SystemEnergyCarrierKind, IReadOnlyList<double>>(),
                Co2ByCarrierKg: new Dictionary<SystemEnergyCarrierKind, IReadOnlyList<double>>(),
                MonthlyFinalEnergyKWh: Enumerable.Repeat(1234.0 / 12.0, 12).ToArray(),
                AnnualSummary: new SystemEnergyAnnualSummary(
                    UsefulEnergyKWh: 1000.0,
                    SystemLoadKWh: 1100.0,
                    EmissionLossesKWh: 0.0,
                    DistributionLossesKWh: 0.0,
                    StorageLossesKWh: 0.0,
                    GenerationLossesKWh: 0.0,
                    RecoveredLossesKWh: 0.0,
                    AuxiliaryEnergyKWh: 0.0,
                    FinalEnergyKWh: 1234.0,
                    PrimaryEnergyKWh: 2468.0,
                    Co2Kg: 493.6),
                Assumptions: ["stub assumption"],
                Warnings: ["stub warning"],
                Diagnostics:
                [
                    new StandardCalculationDiagnostic(
                        CalculationDiagnosticSeverity.Warning,
                        "SYS_STUB_WARNING",
                        "Stub system-energy diagnostic.",
                        Context: "system_energy.heating_annual_kwh")
                ]);
        }
    }
}