using AssistantEngineer.Modules.Calculations.Application.Abstractions.DomesticHotWater;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Services.DomesticHotWater;

public sealed class DomesticHotWaterDistributionLossCalculator : IDomesticHotWaterDistributionLossCalculator
{
    public DomesticHotWaterLossComponentResult Calculate(
        DomesticHotWaterUsefulDemandResult usefulDemand,
        DomesticHotWaterDistributionLossInput input,
        double defaultAmbientTemperatureCelsius,
        double defaultRecoverableFraction)
    {
        ArgumentNullException.ThrowIfNull(usefulDemand);
        ArgumentNullException.ThrowIfNull(input);

        if (!input.IsDistributionPresent)
        {
            return DomesticHotWaterLossProfileHelper.CreateZeroComponent(
                kind: DomesticHotWaterLossComponentKind.Distribution,
                recoveryMode: input.RecoveryMode,
                code: "AE-DHW-DISTRIBUTION-NOT-PRESENT",
                message: "Distribution is not present; distribution losses are zero.",
                source: "DomesticHotWaterDistributionLossCalculator");
        }

        if (input.PipeLengthMeters is not > 0.0 ||
            input.PipeLinearLossCoefficientWPerMeterKelvin is not > 0.0 ||
            input.SupplyTemperatureCelsius is null ||
            !double.IsFinite(input.SupplyTemperatureCelsius.Value))
        {
            return DomesticHotWaterLossProfileHelper.CreateZeroComponent(
                kind: DomesticHotWaterLossComponentKind.Distribution,
                recoveryMode: input.RecoveryMode,
                code: "AE-DHW-DISTRIBUTION-LOSS-NOT-CALCULABLE",
                message: "Distribution losses could not be calculated from provided data.",
                source: "DomesticHotWaterDistributionLossCalculator");
        }

        var diagnostics = new List<StandardCalculationDiagnostic>();
        diagnostics.AddRange(input.Diagnostics);

        var operation = DomesticHotWaterLossProfileHelper.ResolveDistributionOperationProfileFromDemandShape(
            usefulDemand.HourlyVolumeLiters8760,
            input.OperatingHoursPerDay,
            "DomesticHotWaterDistributionLossCalculator",
            diagnostics);
        var ambient = DomesticHotWaterLossProfileHelper.ResolveAmbientProfile(
            input.HourlyAmbientTemperaturesCelsius8760,
            input.AmbientTemperatureCelsius,
            defaultAmbientTemperatureCelsius,
            "DomesticHotWaterDistributionLossCalculator",
            diagnostics);

        var hourlyLoss = ambient
            .Select((ambientTemperature, index) =>
            {
                var deltaT = Math.Max(input.SupplyTemperatureCelsius.Value - ambientTemperature, 0.0);
                var lossW = input.PipeLengthMeters.Value *
                            input.PipeLinearLossCoefficientWPerMeterKelvin.Value *
                            deltaT;
                return lossW * operation[index] / 1000.0;
            })
            .ToArray();

        var recoverableFraction = DomesticHotWaterLossProfileHelper.ResolveRecoverableFraction(
            input.RecoverableFraction,
            defaultRecoverableFraction,
            "DomesticHotWaterDistributionLossCalculator",
            diagnostics);
        var split = DomesticHotWaterLossProfileHelper.SplitRecoverable(hourlyLoss, recoverableFraction);
        var monthlyLoss = DomesticHotWaterLossProfileHelper.BuildMonthlyFromHourly(
            hourlyLoss,
            "DomesticHotWaterDistributionLossCalculator",
            diagnostics);

        diagnostics.Add(CreateInfo(
            "AE-DHW-DISTRIBUTION-LOSS-CALCULATED",
            "Distribution losses were calculated."));

        return new DomesticHotWaterLossComponentResult(
            ComponentKind: DomesticHotWaterLossComponentKind.Distribution,
            AnnualLossKWh: hourlyLoss.Sum(),
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
            "DomesticHotWaterDistributionLossCalculator");
}
