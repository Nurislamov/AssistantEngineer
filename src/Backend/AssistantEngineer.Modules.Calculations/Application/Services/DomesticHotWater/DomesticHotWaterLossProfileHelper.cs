using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Services.DomesticHotWater;

internal static class DomesticHotWaterLossProfileHelper
{
    private static readonly int[] DaysPerMonth = [31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31];

    public static IReadOnlyList<double> ResolveOperationProfile(
        IReadOnlyList<double>? hourlyOperationFractions8760,
        double? operatingHoursPerDay,
        bool defaultToContinuousWhenMissing,
        string diagnosticSource,
        ICollection<StandardCalculationDiagnostic> diagnostics,
        string hourlyUsedDiagnosticCode,
        string flatUsedDiagnosticCode,
        string defaultedDiagnosticCode)
    {
        if (hourlyOperationFractions8760 is { Count: 8760 } &&
            hourlyOperationFractions8760.All(value => double.IsFinite(value) && value >= 0.0))
        {
            diagnostics.Add(CreateInfo(
                hourlyUsedDiagnosticCode,
                "Hourly operation profile was used directly.",
                diagnosticSource));

            return hourlyOperationFractions8760
                .Select(value => Math.Clamp(value, 0.0, 1.0))
                .ToArray();
        }

        if (operatingHoursPerDay is >= 0.0 and <= 24.0)
        {
            var fraction = operatingHoursPerDay.Value / 24.0;
            diagnostics.Add(CreateInfo(
                flatUsedDiagnosticCode,
                $"Flat operation profile derived from OperatingHoursPerDay ({operatingHoursPerDay.Value:F3} h/day).",
                diagnosticSource));
            return Enumerable.Repeat(fraction, 8760).ToArray();
        }

        var defaultHours = defaultToContinuousWhenMissing ? 24.0 : 0.0;
        var defaultFraction = defaultHours / 24.0;
        diagnostics.Add(CreateInfo(
            defaultedDiagnosticCode,
            $"Operation profile defaulted to {defaultHours:F1} h/day.",
            diagnosticSource));
        return Enumerable.Repeat(defaultFraction, 8760).ToArray();
    }

    public static IReadOnlyList<double> ResolveDistributionOperationProfileFromDemandShape(
        IReadOnlyList<double> usefulDemandHourlyVolumeLiters8760,
        double? operatingHoursPerDay,
        string diagnosticSource,
        ICollection<StandardCalculationDiagnostic> diagnostics)
    {
        if (usefulDemandHourlyVolumeLiters8760.Count == 8760 &&
            usefulDemandHourlyVolumeLiters8760.Any(value => value > 0.0))
        {
            var max = usefulDemandHourlyVolumeLiters8760.Max();
            if (max > 0.0)
            {
                diagnostics.Add(CreateInfo(
                    "AE-DHW-DISTRIBUTION-DEMAND-SHAPE-USED",
                    "Distribution operation profile was derived from useful-demand hourly volume shape.",
                    diagnosticSource));
                return usefulDemandHourlyVolumeLiters8760
                    .Select(value => Math.Clamp(value / max, 0.0, 1.0))
                    .ToArray();
            }
        }

        var fraction = operatingHoursPerDay is >= 0.0 and <= 24.0
            ? operatingHoursPerDay.Value / 24.0
            : 1.0;
        diagnostics.Add(CreateInfo(
            "AE-DHW-DISTRIBUTION-FLAT-OPERATION-USED",
            "Distribution operation profile used a flat fraction profile.",
            diagnosticSource));
        return Enumerable.Repeat(fraction, 8760).ToArray();
    }

    public static IReadOnlyList<double> ResolveAmbientProfile(
        IReadOnlyList<double>? hourlyAmbientTemperaturesCelsius8760,
        double? scalarAmbientTemperatureCelsius,
        double defaultAmbientTemperatureCelsius,
        string diagnosticSource,
        ICollection<StandardCalculationDiagnostic> diagnostics)
    {
        if (hourlyAmbientTemperaturesCelsius8760 is { Count: 8760 } &&
            hourlyAmbientTemperaturesCelsius8760.All(double.IsFinite))
        {
            return hourlyAmbientTemperaturesCelsius8760.ToArray();
        }

        if (scalarAmbientTemperatureCelsius is not null && double.IsFinite(scalarAmbientTemperatureCelsius.Value))
        {
            return Enumerable.Repeat(scalarAmbientTemperatureCelsius.Value, 8760).ToArray();
        }

        diagnostics.Add(CreateInfo(
            "AE-DHW-LOSS-AMBIENT-TEMPERATURE-DEFAULTED",
            $"Ambient temperature defaulted to {defaultAmbientTemperatureCelsius:F2} C.",
            diagnosticSource));
        return Enumerable.Repeat(defaultAmbientTemperatureCelsius, 8760).ToArray();
    }

    public static double ResolveRecoverableFraction(
        double? recoverableFraction,
        double defaultRecoverableFraction,
        string diagnosticSource,
        ICollection<StandardCalculationDiagnostic> diagnostics)
    {
        if (recoverableFraction is >= 0.0 and <= 1.0)
            return recoverableFraction.Value;

        diagnostics.Add(CreateInfo(
            "AE-DHW-LOSS-RECOVERABLE-FRACTION-DEFAULTED",
            $"Recoverable fraction defaulted to {defaultRecoverableFraction:F3}.",
            diagnosticSource));
        return Math.Clamp(defaultRecoverableFraction, 0.0, 1.0);
    }

    public static (
        IReadOnlyList<double> hourlyRecoverable,
        IReadOnlyList<double> hourlyNonRecoverable,
        double annualRecoverable,
        double annualNonRecoverable)
        SplitRecoverable(
            IReadOnlyList<double> hourlyLossKwh,
            double recoverableFraction)
    {
        var recoverable = hourlyLossKwh
            .Select(value => value * recoverableFraction)
            .ToArray();
        var nonRecoverable = hourlyLossKwh
            .Select((value, index) => value - recoverable[index])
            .ToArray();
        return (recoverable, nonRecoverable, recoverable.Sum(), nonRecoverable.Sum());
    }

    public static IReadOnlyList<double> BuildMonthlyFromHourly(
        IReadOnlyList<double> hourlyValues,
        string diagnosticSource,
        ICollection<StandardCalculationDiagnostic> diagnostics)
    {
        var monthly = new double[12];
        var offset = 0;
        for (var month = 0; month < 12; month++)
        {
            var hours = DaysPerMonth[month] * 24;
            monthly[month] = hourlyValues.Skip(offset).Take(hours).Sum();
            offset += hours;
        }

        diagnostics.Add(CreateInfo(
            "AE-DHW-LOSS-MONTHLY-AGGREGATED",
            "Monthly values were aggregated from the 8760 hourly profile.",
            diagnosticSource));
        return monthly;
    }

    public static DomesticHotWaterLossComponentResult CreateZeroComponent(
        DomesticHotWaterLossComponentKind kind,
        DomesticHotWaterLossRecoveryMode recoveryMode,
        string code,
        string message,
        string source)
    {
        var diagnostics = new[]
        {
            CreateInfo(code, message, source)
        };

        return new DomesticHotWaterLossComponentResult(
            ComponentKind: kind,
            AnnualLossKWh: 0.0,
            MonthlyLossKWh: new double[12],
            HourlyLossKWh8760: new double[8760],
            AnnualRecoverableLossKWh: 0.0,
            AnnualNonRecoverableLossKWh: 0.0,
            HourlyRecoverableLossKWh8760: new double[8760],
            HourlyNonRecoverableLossKWh8760: new double[8760],
            RecoveryMode: recoveryMode,
            Diagnostics: diagnostics);
    }

    private static StandardCalculationDiagnostic CreateInfo(
        string code,
        string message,
        string source) =>
        DomesticHotWaterDiagnosticsFactory.Create(
            CalculationDiagnosticSeverity.Info,
            code,
            message,
            StandardCalculationStage.DomesticHotWater,
            source);
}
