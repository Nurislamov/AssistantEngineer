using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Ventilation;

public sealed class VentilationAndInfiltrationLoadEngine
{
    public Result<VentilationAndInfiltrationLoadResult> Calculate(
        VentilationAndInfiltrationLoadInput input)
    {
        if (input is null)
            return Result<VentilationAndInfiltrationLoadResult>.Validation("Ventilation and infiltration load input is required.");

        var diagnostics = VentilationInputNormalizer.Validate(input);
        var normalization = VentilationInputNormalizer.ResolveAirConstants(input, diagnostics);

        var mechanicalAirflow = MechanicalVentilationScenarioEvaluator.Evaluate(input, diagnostics);
        var infiltrationAirflow = InfiltrationScenarioEvaluator.Evaluate(input, diagnostics);
        var naturalAirflow = NaturalVentilationScenarioEvaluator.Evaluate(input, diagnostics);

        if (diagnostics.Any(diagnostic =>
                diagnostic.Severity == CalculationDiagnosticSeverity.Error))
        {
            return Result<VentilationAndInfiltrationLoadResult>.Success(
                CreateResult(
                    input,
                    normalization.AirDensityKgPerM3,
                    normalization.AirSpecificHeatJPerKgK,
                    MechanicalVentilationLoadResultZero(input.HeatRecoveryEfficiency ?? 0),
                    InfiltrationLoadResultZero(),
                    NaturalVentilationLoadResultZero(),
                    diagnostics));
        }

        var mechanical = CalculateMechanical(
            input,
            normalization.AirDensityKgPerM3,
            normalization.AirSpecificHeatJPerKgK,
            mechanicalAirflow);

        var infiltration = CalculateInfiltration(
            input,
            normalization.AirDensityKgPerM3,
            normalization.AirSpecificHeatJPerKgK,
            infiltrationAirflow);

        var natural = CalculateNaturalVentilation(
            input,
            normalization.AirDensityKgPerM3,
            normalization.AirSpecificHeatJPerKgK,
            naturalAirflow.Airflow,
            naturalAirflow.EnhancedResult);

        return Result<VentilationAndInfiltrationLoadResult>.Success(
            CreateResult(
                input,
                normalization.AirDensityKgPerM3,
                normalization.AirSpecificHeatJPerKgK,
                mechanical,
                infiltration,
                natural,
                diagnostics));
    }

    private static VentilationAndInfiltrationLoadResult CreateResult(
        VentilationAndInfiltrationLoadInput input,
        double airDensity,
        double airSpecificHeat,
        MechanicalVentilationLoadResult mechanical,
        InfiltrationLoadResult infiltration,
        NaturalVentilationLoadResult natural,
        IReadOnlyList<CalculationDiagnostic> diagnostics)
    {
        var totalHeating =
            mechanical.EffectiveHeatingLoadW +
            infiltration.HeatingLoadW +
            natural.HeatingLoadW;

        var totalCooling =
            mechanical.EffectiveCoolingLoadW +
            infiltration.CoolingLoadW +
            natural.CoolingLoadW;

        return new VentilationAndInfiltrationLoadResult(
            input.RoomId,
            input.IndoorTemperatureC,
            input.OutdoorTemperatureC,
            DeltaTC: Round(input.IndoorTemperatureC - input.OutdoorTemperatureC),
            Round(airDensity),
            Round(airSpecificHeat),
            mechanical,
            infiltration,
            natural,
            Round(totalHeating),
            Round(totalCooling),
            SignedHeatFlowW: Round(totalHeating - totalCooling),
            diagnostics);
    }

    private static MechanicalVentilationLoadResult CalculateMechanical(
        VentilationAndInfiltrationLoadInput input,
        double airDensity,
        double airSpecificHeat,
        VentilationAirflowResult airflow)
    {
        var loads = CalculateHeatingCoolingLoads(
            input,
            airflow.AirflowM3PerSecond,
            airDensity,
            airSpecificHeat);

        var heatRecoveryEfficiency = input.HeatRecoveryEfficiency ?? 0.0;
        var heatRecoveryFactor = 1.0 - heatRecoveryEfficiency;

        return new MechanicalVentilationLoadResult(
            Round(airflow.AirflowM3PerHour),
            Round(airflow.AirflowM3PerSecond),
            Round(loads.HeatingLoadW),
            Round(loads.CoolingLoadW),
            Round(heatRecoveryEfficiency),
            Round(loads.HeatingLoadW * heatRecoveryFactor),
            Round(loads.CoolingLoadW * heatRecoveryFactor));
    }

    private static InfiltrationLoadResult CalculateInfiltration(
        VentilationAndInfiltrationLoadInput input,
        double airDensity,
        double airSpecificHeat,
        VentilationAirflowResult airflow)
    {
        var loads = CalculateHeatingCoolingLoads(
            input,
            airflow.AirflowM3PerSecond,
            airDensity,
            airSpecificHeat);

        return new InfiltrationLoadResult(
            Round(airflow.AirChangesPerHour),
            Round(airflow.AirflowM3PerHour),
            Round(airflow.AirflowM3PerSecond),
            Round(loads.HeatingLoadW),
            Round(loads.CoolingLoadW));
    }

    private static NaturalVentilationLoadResult CalculateNaturalVentilation(
        VentilationAndInfiltrationLoadInput input,
        double airDensity,
        double airSpecificHeat,
        VentilationAirflowResult airflow,
        Contracts.Ventilation.Iso16798.Iso16798NaturalVentilationResult? enhancedResult)
    {
        var loads = CalculateHeatingCoolingLoads(
            input,
            airflow.AirflowM3PerSecond,
            airDensity,
            airSpecificHeat);

        return new NaturalVentilationLoadResult(
            Round(airflow.AirflowM3PerHour),
            Round(airflow.AirflowM3PerSecond),
            Round(loads.HeatingLoadW),
            Round(loads.CoolingLoadW),
            AirChangeRatePerHour: Round(enhancedResult?.AirChangeRatePerHour ?? airflow.AirChangesPerHour),
            HeatTransferCoefficientWPerK: Round(enhancedResult?.HeatTransferCoefficientWPerK ?? airDensity * airSpecificHeat * airflow.AirflowM3PerSecond),
            WindComponentM3PerHour: Round(enhancedResult?.WindComponentM3PerHour ?? 0.0),
            StackComponentM3PerHour: Round(enhancedResult?.StackComponentM3PerHour ?? 0.0),
            SelectedBranch: enhancedResult?.SelectedBranch ?? "CompatibilityNaturalVentilationInput",
            ClampReason: enhancedResult?.ClampReason,
            ControlReason: enhancedResult?.ControlReason);
    }

    private static HeatingCoolingLoad CalculateHeatingCoolingLoads(
        VentilationAndInfiltrationLoadInput input,
        double airflowM3PerSecond,
        double airDensity,
        double airSpecificHeat)
    {
        var airflowLoadPerK =
            airDensity *
            airSpecificHeat *
            airflowM3PerSecond;

        var deltaHeating = Math.Max(
            input.IndoorTemperatureC - input.OutdoorTemperatureC,
            0.0);

        var deltaCooling = Math.Max(
            input.OutdoorTemperatureC - input.IndoorTemperatureC,
            0.0);

        return new HeatingCoolingLoad(
            airflowLoadPerK * deltaHeating,
            airflowLoadPerK * deltaCooling);
    }

    private static MechanicalVentilationLoadResult MechanicalVentilationLoadResultZero(
        double heatRecoveryEfficiency) =>
        new(
            AirflowM3PerHour: 0,
            AirflowM3PerSecond: 0,
            RawHeatingLoadW: 0,
            RawCoolingLoadW: 0,
            Math.Clamp(heatRecoveryEfficiency, 0, 1),
            EffectiveHeatingLoadW: 0,
            EffectiveCoolingLoadW: 0);

    private static InfiltrationLoadResult InfiltrationLoadResultZero() =>
        new(
            InfiltrationAirChangesPerHour: 0,
            InfiltrationAirflowM3PerHour: 0,
            InfiltrationAirflowM3PerSecond: 0,
            HeatingLoadW: 0,
            CoolingLoadW: 0);

    private static NaturalVentilationLoadResult NaturalVentilationLoadResultZero() =>
        new(
            AirflowM3PerHour: 0,
            AirflowM3PerSecond: 0,
            HeatingLoadW: 0,
            CoolingLoadW: 0);

    private static double Round(
        double value) =>
        Math.Round(value, 6, MidpointRounding.AwayFromZero);

    private sealed record HeatingCoolingLoad(
        double HeatingLoadW,
        double CoolingLoadW);
}
