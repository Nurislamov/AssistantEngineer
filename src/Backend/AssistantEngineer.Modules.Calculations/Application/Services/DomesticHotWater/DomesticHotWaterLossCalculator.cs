using AssistantEngineer.Modules.Calculations.Application.Abstractions.DomesticHotWater;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Services.DomesticHotWater;

public sealed class DomesticHotWaterLossCalculator : IDomesticHotWaterLossCalculator
{
    public DomesticHotWaterLossResult Calculate(
        IReadOnlyList<double> usefulDemandProfileKWh,
        DomesticHotWaterLossDefinition lossDefinition,
        IReadOnlyList<double>? hotWaterSetpointProfileCelsius)
    {
        ArgumentNullException.ThrowIfNull(usefulDemandProfileKWh);
        ArgumentNullException.ThrowIfNull(lossDefinition);

        var assumptions = new List<string>();
        var warnings = new List<string>();
        var diagnostics = new List<StandardCalculationDiagnostic>();
        diagnostics.AddRange(lossDefinition.Diagnostics);

        var stepCount = usefulDemandProfileKWh.Count;
        var timeStepHours = lossDefinition.TimeStepHours > 0.0 && double.IsFinite(lossDefinition.TimeStepHours)
            ? lossDefinition.TimeStepHours
            : 1.0;
        if (timeStepHours != lossDefinition.TimeStepHours)
        {
            warnings.Add("Loss timestep defaulted to 1h because provided value was invalid.");
            diagnostics.Add(CreateWarning(
                "AE-DHW-LOSS-TIMESTEP-DEFAULTED",
                "Loss timestep hours should be positive and finite."));
        }

        var hotProfile = hotWaterSetpointProfileCelsius is { Count: > 0 }
            ? EnsureLength(hotWaterSetpointProfileCelsius, stepCount)
            : Enumerable.Repeat(55.0, stepCount).ToArray();

        var ambientStorage = Enumerable.Repeat(lossDefinition.StorageAmbientTemperatureCelsius ?? 20.0, stepCount).ToArray();
        var ambientDistribution = Enumerable.Repeat(lossDefinition.StorageAmbientTemperatureCelsius ?? 20.0, stepCount).ToArray();
        var operationProfile = ResolveOperationProfile(lossDefinition, stepCount);

        var storage = new double[stepCount];
        var distribution = new double[stepCount];
        var circulation = new double[stepCount];
        var auxiliary = new double[stepCount];

        if (lossDefinition.LossOwnershipPolicy == DomesticHotWaterLossOwnershipPolicy.SystemEnergyOwnLosses)
        {
            assumptions.Add("System-energy chain owns DHW technical losses; DHW-side technical loss profiles were set to zero.");
            diagnostics.Add(CreateInfo(
                "AE-DHW-LOSS-OWNERSHIP-SYSTEM-ENERGY",
                "DHW technical losses were skipped because ownership is assigned to system-energy chain."));
        }
        else
        {
            CalculateStorageLosses(lossDefinition, timeStepHours, hotProfile, ambientStorage, storage, warnings, diagnostics);
            CalculateDistributionLosses(lossDefinition, timeStepHours, hotProfile, ambientDistribution, operationProfile, distribution, warnings, diagnostics);
            CalculateCirculationLosses(lossDefinition, timeStepHours, hotProfile, ambientDistribution, operationProfile, circulation, warnings, diagnostics);
        }

        if (lossDefinition.AuxiliaryEnergyProfileKWh is { Count: > 0 } auxiliaryProfile)
        {
            auxiliary = EnsureLength(auxiliaryProfile, stepCount)
                .Select(value => Math.Max(0.0, value))
                .ToArray();
        }
        else if (lossDefinition.AuxiliaryEnergyPerStepKWh is > 0.0)
        {
            auxiliary = Enumerable.Repeat(lossDefinition.AuxiliaryEnergyPerStepKWh.Value, stepCount).ToArray();
        }

        var recoveredFraction = lossDefinition.RecoveredLossFraction;
        if (!double.IsFinite(recoveredFraction) || recoveredFraction < 0.0 || recoveredFraction > 1.0)
        {
            diagnostics.Add(CreateError(
                "AE-DHW-LOSS-RECOVERED-FRACTION-OUT-OF-RANGE",
                "Recovered-loss fraction must be within [0, 1]."));
            recoveredFraction = Math.Clamp(recoveredFraction, 0.0, 1.0);
        }

        var recovered = new double[stepCount];
        for (var index = 0; index < stepCount; index++)
        {
            var thermalLoss = storage[index] + distribution[index] + circulation[index];
            recovered[index] = thermalLoss * recoveredFraction;
        }

        assumptions.Add("Recovered losses reduce system load and are not added to auxiliary electricity.");

        return new DomesticHotWaterLossResult(
            StorageLossesProfileKWh: storage,
            DistributionLossesProfileKWh: distribution,
            CirculationLossesProfileKWh: circulation,
            RecoveredLossesProfileKWh: recovered,
            AuxiliaryEnergyProfileKWh: auxiliary,
            Assumptions: assumptions.ToArray(),
            Warnings: warnings.ToArray(),
            Diagnostics: SortDiagnostics(diagnostics));
    }

    private static void CalculateStorageLosses(
        DomesticHotWaterLossDefinition lossDefinition,
        double timeStepHours,
        IReadOnlyList<double> hotProfile,
        IReadOnlyList<double> ambientProfile,
        double[] storage,
        ICollection<string> warnings,
        ICollection<StandardCalculationDiagnostic> diagnostics)
    {
        if (lossDefinition.StorageLossCoefficientWPerKelvin is not > 0.0)
        {
            warnings.Add("Storage loss coefficient missing; storage losses were set to zero.");
            diagnostics.Add(CreateInfo(
                "AE-DHW-STORAGE-LOSS-FALLBACK-ZERO",
                "Storage losses were set to zero because no valid storage coefficient was provided."));
            return;
        }

        for (var index = 0; index < storage.Length; index++)
        {
            var deltaT = Math.Max(0.0, hotProfile[index] - ambientProfile[index]);
            storage[index] = lossDefinition.StorageLossCoefficientWPerKelvin.Value * deltaT * timeStepHours / 1000.0;
        }
    }

    private static void CalculateDistributionLosses(
        DomesticHotWaterLossDefinition lossDefinition,
        double timeStepHours,
        IReadOnlyList<double> hotProfile,
        IReadOnlyList<double> ambientProfile,
        IReadOnlyList<double> operationProfile,
        double[] distribution,
        ICollection<string> warnings,
        ICollection<StandardCalculationDiagnostic> diagnostics)
    {
        if (lossDefinition.DistributionPipeLengthMeters is not > 0.0 ||
            lossDefinition.DistributionLossCoefficientWPerMeterKelvin is not > 0.0)
        {
            warnings.Add("Distribution parameters missing; distribution losses were set to zero.");
            diagnostics.Add(CreateInfo(
                "AE-DHW-DISTRIBUTION-LOSS-FALLBACK-ZERO",
                "Distribution losses were set to zero because required distribution parameters were not provided."));
            return;
        }

        for (var index = 0; index < distribution.Length; index++)
        {
            var deltaT = Math.Max(0.0, hotProfile[index] - ambientProfile[index]);
            var lossW = lossDefinition.DistributionPipeLengthMeters.Value *
                        lossDefinition.DistributionLossCoefficientWPerMeterKelvin.Value *
                        deltaT;
            distribution[index] = lossW * operationProfile[index] * timeStepHours / 1000.0;
        }
    }

    private static void CalculateCirculationLosses(
        DomesticHotWaterLossDefinition lossDefinition,
        double timeStepHours,
        IReadOnlyList<double> hotProfile,
        IReadOnlyList<double> ambientProfile,
        IReadOnlyList<double> operationProfile,
        double[] circulation,
        ICollection<string> warnings,
        ICollection<StandardCalculationDiagnostic> diagnostics)
    {
        if (lossDefinition.SystemKind == DomesticHotWaterSystemKind.NoCirculation)
            return;

        if (lossDefinition.CirculationLoopLengthMeters is not > 0.0 ||
            lossDefinition.CirculationLossCoefficientWPerMeterKelvin is not > 0.0)
        {
            warnings.Add("Circulation parameters missing; circulation losses were set to zero.");
            diagnostics.Add(CreateInfo(
                "AE-DHW-CIRCULATION-LOSS-FALLBACK-ZERO",
                "Circulation losses were set to zero because required circulation parameters were not provided."));
            return;
        }

        for (var index = 0; index < circulation.Length; index++)
        {
            var deltaT = Math.Max(0.0, hotProfile[index] - ambientProfile[index]);
            var lossW = lossDefinition.CirculationLoopLengthMeters.Value *
                        lossDefinition.CirculationLossCoefficientWPerMeterKelvin.Value *
                        deltaT;
            circulation[index] = lossW * operationProfile[index] * timeStepHours / 1000.0;
        }
    }

    private static IReadOnlyList<double> ResolveOperationProfile(
        DomesticHotWaterLossDefinition lossDefinition,
        int stepCount)
    {
        if (lossDefinition.CirculationOperationSchedule is { Count: > 0 })
        {
            var resized = EnsureLength(lossDefinition.CirculationOperationSchedule, stepCount);
            return resized.Select(value => Math.Clamp(value, 0.0, 1.0)).ToArray();
        }

        var operation = Math.Clamp(lossDefinition.CirculationOperationFraction ?? 1.0, 0.0, 1.0);
        return Enumerable.Repeat(operation, stepCount).ToArray();
    }

    private static double[] EnsureLength(IReadOnlyList<double> profile, int stepCount)
    {
        if (profile.Count == stepCount)
            return profile.ToArray();

        if (profile.Count <= 0)
            return new double[stepCount];

        var result = new double[stepCount];
        for (var index = 0; index < stepCount; index++)
            result[index] = profile[index % profile.Count];

        return result;
    }

    private static IReadOnlyList<StandardCalculationDiagnostic> SortDiagnostics(
        IEnumerable<StandardCalculationDiagnostic> diagnostics) =>
        diagnostics
            .OrderByDescending(item => item.Severity)
            .ThenBy(item => item.Code, StringComparer.Ordinal)
            .ThenBy(item => item.Message, StringComparer.Ordinal)
            .ToArray();

    private static StandardCalculationDiagnostic CreateInfo(
        string code,
        string message) =>
        DomesticHotWaterDiagnosticsFactory.Create(
            CalculationDiagnosticSeverity.Info,
            code,
            message,
            StandardCalculationStage.DomesticHotWater,
            "DomesticHotWaterLossCalculator");

    private static StandardCalculationDiagnostic CreateWarning(
        string code,
        string message) =>
        DomesticHotWaterDiagnosticsFactory.Create(
            CalculationDiagnosticSeverity.Warning,
            code,
            message,
            StandardCalculationStage.DomesticHotWater,
            "DomesticHotWaterLossCalculator");

    private static StandardCalculationDiagnostic CreateError(
        string code,
        string message) =>
        DomesticHotWaterDiagnosticsFactory.Create(
            CalculationDiagnosticSeverity.Error,
            code,
            message,
            StandardCalculationStage.DomesticHotWater,
            "DomesticHotWaterLossCalculator");
}
