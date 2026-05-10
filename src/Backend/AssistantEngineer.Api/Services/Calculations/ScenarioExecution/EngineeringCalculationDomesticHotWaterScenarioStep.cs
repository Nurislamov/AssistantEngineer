using System.Globalization;
using AssistantEngineer.Api.Contracts.Calculations;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.DomesticHotWater;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Api.Services.Calculations;

public sealed class EngineeringCalculationDomesticHotWaterScenarioStep : IEngineeringCalculationDomesticHotWaterScenarioStep
{
    private readonly IDomesticHotWaterSystemLoadCalculator _domesticHotWaterSystemLoadCalculator;

    public EngineeringCalculationDomesticHotWaterScenarioStep(
        IDomesticHotWaterSystemLoadCalculator domesticHotWaterSystemLoadCalculator)
    {
        _domesticHotWaterSystemLoadCalculator = domesticHotWaterSystemLoadCalculator;
    }

    public EngineeringCalculationDomesticHotWaterScenarioStepResult Execute(
        EngineeringCalculationScenarioRequestDto request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.State);

        var annualUseful = ResolveAnnualValue(request.State.Metadata, "dhw.useful_annual_kwh");
        if (!(annualUseful > 0.0))
        {
            return new EngineeringCalculationDomesticHotWaterScenarioStepResult(
                ScenarioModuleExecution.Skip(
                    "DHW annual useful demand is not provided in workflow metadata (`dhw.useful_annual_kwh`).",
                    "Provide structured DHW useful demand metadata to execute DHW module."),
                Summary: null,
                Assumptions: [],
                Warnings: [],
                Diagnostics: []);
        }

        var usefulProfile = BuildFlatProfile8760(annualUseful.Value);
        var lossDefinition = new DomesticHotWaterLossDefinition(
            SystemKind: DomesticHotWaterSystemKind.CentralStorage,
            StorageVolumeLiters: 300,
            StorageLossCoefficientWPerKelvin: 2.0,
            StorageAmbientTemperatureCelsius: 20.0,
            DistributionPipeLengthMeters: 40.0,
            DistributionLossCoefficientWPerMeterKelvin: 0.12,
            CirculationOperationSchedule: null,
            CirculationOperationFraction: 0.6,
            CirculationLoopLengthMeters: 15.0,
            CirculationLossCoefficientWPerMeterKelvin: 0.15,
            RecoveredLossFraction: 0.2,
            AuxiliaryEnergyProfileKWh: null,
            AuxiliaryEnergyPerStepKWh: 0.01,
            LossOwnershipPolicy: DomesticHotWaterLossOwnershipPolicy.DhwOwnLosses,
            TimeStepHours: 1.0,
            Source: "EngineeringCalculationDomesticHotWaterScenarioStep",
            Diagnostics: []);

        var summary = _domesticHotWaterSystemLoadCalculator.Calculate(
            new DomesticHotWaterSystemLoadRequest(
                UsefulDemandProfileKWh: usefulProfile,
                LossDefinition: lossDefinition,
                ColdWaterTemperatureProfileCelsius: null,
                HotWaterSetpointProfileCelsius: null,
                TimeStepHours: 1.0));

        return new EngineeringCalculationDomesticHotWaterScenarioStepResult(
            ScenarioModuleExecution.Execute(
            [
                new EngineeringCalculationModuleValueDto("dhw_annual_useful_kwh", "Annual useful DHW demand", summary.AnnualSummary.UsefulEnergyKWh, "kWh"),
                new EngineeringCalculationModuleValueDto("dhw_annual_system_kwh", "Annual DHW system heat", summary.AnnualSummary.SystemLoadKWh, "kWh"),
                new EngineeringCalculationModuleValueDto("dhw_annual_losses_kwh", "Annual DHW losses", summary.AnnualSummary.StorageLossesKWh + summary.AnnualSummary.DistributionLossesKWh + summary.AnnualSummary.CirculationLossesKWh, "kWh")
            ], "IDomesticHotWaterSystemLoadCalculator"),
            Summary: summary,
            Assumptions: summary.Assumptions,
            Warnings: summary.Warnings,
            Diagnostics: summary.Diagnostics.Select(item =>
                FromStandardDiagnostic(item, "DomesticHotWater", "DhwFoundation")).ToArray());
    }

    private static double[] BuildFlatProfile8760(double annualValueKwh)
    {
        var safeAnnual = double.IsFinite(annualValueKwh) && annualValueKwh > 0.0
            ? annualValueKwh
            : 0.0;
        var hourly = safeAnnual / 8760.0;
        return Enumerable.Repeat(hourly, 8760).ToArray();
    }

    private static double? ResolveAnnualValue(
        IReadOnlyDictionary<string, string> metadata,
        string key)
    {
        if (!metadata.TryGetValue(key, out var raw))
            return null;

        if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var invariantParsed) &&
            double.IsFinite(invariantParsed) &&
            invariantParsed > 0.0)
        {
            return invariantParsed;
        }

        if (double.TryParse(raw, out var parsed) && double.IsFinite(parsed) && parsed > 0.0)
            return parsed;

        return null;
    }

    private static EngineeringWorkflowDiagnosticDto FromStandardDiagnostic(
        StandardCalculationDiagnostic diagnostic,
        string step,
        string? sourceModule = null)
    {
        return new EngineeringWorkflowDiagnosticDto(
            Severity: diagnostic.Severity switch
            {
                CalculationDiagnosticSeverity.Error => "error",
                CalculationDiagnosticSeverity.Warning => "warning",
                _ => "info"
            },
            Code: diagnostic.Code,
            Message: diagnostic.Message,
            SourceStep: step,
            SourceModule: sourceModule,
            TargetField: diagnostic.Context);
    }
}