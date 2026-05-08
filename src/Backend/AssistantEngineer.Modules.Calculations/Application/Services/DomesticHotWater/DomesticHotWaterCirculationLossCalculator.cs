using AssistantEngineer.Modules.Calculations.Application.Abstractions.DomesticHotWater;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Services.DomesticHotWater;

public sealed class DomesticHotWaterCirculationLossCalculator : IDomesticHotWaterCirculationLossCalculator
{
    public IReadOnlyList<DomesticHotWaterLossComponentResult> Calculate(
        DomesticHotWaterUsefulDemandResult usefulDemand,
        DomesticHotWaterCirculationLossInput input,
        double defaultAmbientTemperatureCelsius,
        double defaultRecoverableFraction)
    {
        ArgumentNullException.ThrowIfNull(usefulDemand);
        ArgumentNullException.ThrowIfNull(input);

        if (!input.IsCirculationPresent)
        {
            return
            [
                DomesticHotWaterLossProfileHelper.CreateZeroComponent(
                    kind: DomesticHotWaterLossComponentKind.Circulation,
                    recoveryMode: input.RecoveryMode,
                    code: "AE-DHW-CIRCULATION-NOT-PRESENT",
                    message: "Circulation is not present; circulation thermal losses are zero.",
                    source: "DomesticHotWaterCirculationLossCalculator"),
                DomesticHotWaterLossProfileHelper.CreateZeroComponent(
                    kind: DomesticHotWaterLossComponentKind.AuxiliaryElectricity,
                    recoveryMode: DomesticHotWaterLossRecoveryMode.NonRecoverable,
                    code: "AE-DHW-CIRCULATION-NOT-PRESENT",
                    message: "Circulation is not present; circulation auxiliary electricity is zero.",
                    source: "DomesticHotWaterCirculationLossCalculator")
            ];
        }

        var diagnostics = new List<StandardCalculationDiagnostic>();
        diagnostics.AddRange(input.Diagnostics);

        var operation = DomesticHotWaterLossProfileHelper.ResolveOperationProfile(
            input.HourlyOperationFractions8760,
            input.OperatingHoursPerDay,
            defaultToContinuousWhenMissing: true,
            diagnosticSource: "DomesticHotWaterCirculationLossCalculator",
            diagnostics: diagnostics,
            hourlyUsedDiagnosticCode: "AE-DHW-CIRCULATION-HOURLY-OPERATION-USED",
            flatUsedDiagnosticCode: "AE-DHW-CIRCULATION-FLAT-OPERATION-USED",
            defaultedDiagnosticCode: "AE-DHW-CIRCULATION-CONTINUOUS-DEFAULTED");
        var ambient = DomesticHotWaterLossProfileHelper.ResolveAmbientProfile(
            input.HourlyAmbientTemperaturesCelsius8760,
            input.AmbientTemperatureCelsius,
            defaultAmbientTemperatureCelsius,
            "DomesticHotWaterCirculationLossCalculator",
            diagnostics);

        DomesticHotWaterLossComponentResult thermalComponent;
        if (input.LoopLengthMeters is > 0.0 &&
            input.LoopLinearLossCoefficientWPerMeterKelvin is > 0.0 &&
            input.SupplyTemperatureCelsius is not null &&
            double.IsFinite(input.SupplyTemperatureCelsius.Value))
        {
            var hourlyLoss = ambient
                .Select((ambientTemperature, index) =>
                {
                    var deltaT = Math.Max(input.SupplyTemperatureCelsius.Value - ambientTemperature, 0.0);
                    var lossW = input.LoopLengthMeters.Value *
                                input.LoopLinearLossCoefficientWPerMeterKelvin.Value *
                                deltaT;
                    return lossW * operation[index] / 1000.0;
                })
                .ToArray();

            var recoverableFraction = DomesticHotWaterLossProfileHelper.ResolveRecoverableFraction(
                input.RecoverableFraction,
                defaultRecoverableFraction,
                "DomesticHotWaterCirculationLossCalculator",
                diagnostics);
            var split = DomesticHotWaterLossProfileHelper.SplitRecoverable(hourlyLoss, recoverableFraction);
            var monthlyLoss = DomesticHotWaterLossProfileHelper.BuildMonthlyFromHourly(
                hourlyLoss,
                "DomesticHotWaterCirculationLossCalculator",
                diagnostics);

            diagnostics.Add(CreateInfo(
                "AE-DHW-CIRCULATION-LOSS-CALCULATED",
                "Circulation thermal losses were calculated."));

            thermalComponent = new DomesticHotWaterLossComponentResult(
                ComponentKind: DomesticHotWaterLossComponentKind.Circulation,
                AnnualLossKWh: hourlyLoss.Sum(),
                MonthlyLossKWh: monthlyLoss,
                HourlyLossKWh8760: hourlyLoss,
                AnnualRecoverableLossKWh: split.annualRecoverable,
                AnnualNonRecoverableLossKWh: split.annualNonRecoverable,
                HourlyRecoverableLossKWh8760: split.hourlyRecoverable,
                HourlyNonRecoverableLossKWh8760: split.hourlyNonRecoverable,
                RecoveryMode: input.RecoveryMode,
                Diagnostics: diagnostics.ToArray());
        }
        else
        {
            thermalComponent = DomesticHotWaterLossProfileHelper.CreateZeroComponent(
                kind: DomesticHotWaterLossComponentKind.Circulation,
                recoveryMode: input.RecoveryMode,
                code: "AE-DHW-CIRCULATION-LOSS-NOT-CALCULABLE",
                message: "Circulation thermal losses could not be calculated from provided data.",
                source: "DomesticHotWaterCirculationLossCalculator");
        }

        var auxiliaryDiagnostics = new List<StandardCalculationDiagnostic>();
        if (input.PumpPowerWatts is > 0.0)
        {
            var hourlyAux = operation
                .Select(fraction => input.PumpPowerWatts.Value * fraction / 1000.0)
                .ToArray();
            var monthlyAux = DomesticHotWaterLossProfileHelper.BuildMonthlyFromHourly(
                hourlyAux,
                "DomesticHotWaterCirculationLossCalculator",
                auxiliaryDiagnostics);
            auxiliaryDiagnostics.Add(CreateInfo(
                "AE-DHW-CIRCULATION-PUMP-AUXILIARY-CALCULATED",
                "Circulation pump auxiliary electricity profile was calculated."));

            var auxiliaryComponent = new DomesticHotWaterLossComponentResult(
                ComponentKind: DomesticHotWaterLossComponentKind.AuxiliaryElectricity,
                AnnualLossKWh: hourlyAux.Sum(),
                MonthlyLossKWh: monthlyAux,
                HourlyLossKWh8760: hourlyAux,
                AnnualRecoverableLossKWh: 0.0,
                AnnualNonRecoverableLossKWh: 0.0,
                HourlyRecoverableLossKWh8760: new double[8760],
                HourlyNonRecoverableLossKWh8760: new double[8760],
                RecoveryMode: DomesticHotWaterLossRecoveryMode.NonRecoverable,
                Diagnostics: auxiliaryDiagnostics);

            return [thermalComponent, auxiliaryComponent];
        }

        var zeroAuxiliaryComponent = DomesticHotWaterLossProfileHelper.CreateZeroComponent(
            kind: DomesticHotWaterLossComponentKind.AuxiliaryElectricity,
            recoveryMode: DomesticHotWaterLossRecoveryMode.NonRecoverable,
            code: "AE-DHW-CIRCULATION-PUMP-AUXILIARY-CALCULATED",
            message: "Circulation pump auxiliary electricity is zero because pump power was not provided.",
            source: "DomesticHotWaterCirculationLossCalculator");
        return [thermalComponent, zeroAuxiliaryComponent];
    }

    private static StandardCalculationDiagnostic CreateInfo(
        string code,
        string message) =>
        DomesticHotWaterDiagnosticsFactory.Create(
            CalculationDiagnosticSeverity.Info,
            code,
            message,
            StandardCalculationStage.DomesticHotWater,
            "DomesticHotWaterCirculationLossCalculator");
}
