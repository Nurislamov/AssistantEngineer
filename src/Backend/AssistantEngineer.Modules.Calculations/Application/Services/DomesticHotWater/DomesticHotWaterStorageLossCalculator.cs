using AssistantEngineer.Modules.Calculations.Application.Abstractions.DomesticHotWater;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Services.DomesticHotWater;

public sealed class DomesticHotWaterStorageLossCalculator : IDomesticHotWaterStorageLossCalculator
{
    public DomesticHotWaterLossComponentResult Calculate(
        DomesticHotWaterUsefulDemandResult usefulDemand,
        DomesticHotWaterStorageLossInput input,
        double defaultAmbientTemperatureCelsius,
        double defaultRecoverableFraction)
    {
        ArgumentNullException.ThrowIfNull(usefulDemand);
        ArgumentNullException.ThrowIfNull(input);

        if (!input.IsStoragePresent)
        {
            return DomesticHotWaterLossProfileHelper.CreateZeroComponent(
                kind: DomesticHotWaterLossComponentKind.Storage,
                recoveryMode: input.RecoveryMode,
                code: "AE-DHW-STORAGE-NOT-PRESENT",
                message: "Storage is not present; storage losses are zero.",
                source: "DomesticHotWaterStorageLossCalculator");
        }

        var diagnostics = new List<StandardCalculationDiagnostic>();
        diagnostics.AddRange(input.Diagnostics);

        var operation = DomesticHotWaterLossProfileHelper.ResolveOperationProfile(
            hourlyOperationFractions8760: null,
            operatingHoursPerDay: input.OperatingHoursPerDay,
            defaultToContinuousWhenMissing: true,
            diagnosticSource: "DomesticHotWaterStorageLossCalculator",
            diagnostics: diagnostics,
            hourlyUsedDiagnosticCode: "AE-DHW-LOSS-OPERATION-PROFILE-DEFAULTED",
            flatUsedDiagnosticCode: "AE-DHW-LOSS-OPERATION-PROFILE-DEFAULTED",
            defaultedDiagnosticCode: "AE-DHW-LOSS-OPERATION-PROFILE-DEFAULTED");

        var ambient = DomesticHotWaterLossProfileHelper.ResolveAmbientProfile(
            input.HourlyAmbientTemperaturesCelsius8760,
            input.AmbientTemperatureCelsius,
            defaultAmbientTemperatureCelsius,
            "DomesticHotWaterStorageLossCalculator",
            diagnostics);

        IReadOnlyList<double> hourlyLoss;
        if (input.StandingLossWatts is > 0.0)
        {
            hourlyLoss = operation
                .Select(fraction => input.StandingLossWatts.Value * fraction / 1000.0)
                .ToArray();
            diagnostics.Add(CreateInfo(
                "AE-DHW-STORAGE-STANDING-LOSS-USED",
                "Storage standing-loss method was used."));
        }
        else if (input.StorageLossCoefficientWPerKelvin is > 0.0 &&
                 input.StorageSetpointTemperatureCelsius is not null &&
                 double.IsFinite(input.StorageSetpointTemperatureCelsius.Value))
        {
            hourlyLoss = ambient
                .Select((ambientTemperature, index) =>
                {
                    var deltaT = Math.Max(input.StorageSetpointTemperatureCelsius.Value - ambientTemperature, 0.0);
                    var lossW = input.StorageLossCoefficientWPerKelvin.Value * deltaT;
                    return lossW * operation[index] / 1000.0;
                })
                .ToArray();
            diagnostics.Add(CreateInfo(
                "AE-DHW-STORAGE-COEFFICIENT-LOSS-USED",
                "Storage coefficient-loss method was used."));
        }
        else
        {
            return DomesticHotWaterLossProfileHelper.CreateZeroComponent(
                kind: DomesticHotWaterLossComponentKind.Storage,
                recoveryMode: input.RecoveryMode,
                code: "AE-DHW-STORAGE-LOSS-NOT-CALCULABLE",
                message: "Storage losses could not be calculated from provided data.",
                source: "DomesticHotWaterStorageLossCalculator");
        }

        var recoverableFraction = DomesticHotWaterLossProfileHelper.ResolveRecoverableFraction(
            input.RecoverableFraction,
            defaultRecoverableFraction,
            "DomesticHotWaterStorageLossCalculator",
            diagnostics);
        var split = DomesticHotWaterLossProfileHelper.SplitRecoverable(hourlyLoss, recoverableFraction);
        var monthlyLoss = DomesticHotWaterLossProfileHelper.BuildMonthlyFromHourly(
            hourlyLoss,
            "DomesticHotWaterStorageLossCalculator",
            diagnostics);
        var annualLoss = hourlyLoss.Sum();

        diagnostics.Add(CreateInfo(
            "AE-DHW-STORAGE-LOSS-CALCULATED",
            "Storage losses were calculated."));

        return new DomesticHotWaterLossComponentResult(
            ComponentKind: DomesticHotWaterLossComponentKind.Storage,
            AnnualLossKWh: annualLoss,
            MonthlyLossKWh: monthlyLoss,
            HourlyLossKWh8760: hourlyLoss,
            AnnualRecoverableLossKWh: split.annualRecoverable,
            AnnualNonRecoverableLossKWh: split.annualNonRecoverable,
            HourlyRecoverableLossKWh8760: split.hourlyRecoverable,
            HourlyNonRecoverableLossKWh8760: split.hourlyNonRecoverable,
            RecoveryMode: input.RecoveryMode,
            Diagnostics: diagnostics);
    }

    private static StandardCalculationDiagnostic CreateInfo(
        string code,
        string message) =>
        DomesticHotWaterDiagnosticsFactory.Create(
            CalculationDiagnosticSeverity.Info,
            code,
            message,
            StandardCalculationStage.DomesticHotWater,
            "DomesticHotWaterStorageLossCalculator");
}
