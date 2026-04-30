using AssistantEngineer.Modules.Calculations.Application.Contracts.AnnualEnergy;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Services.AnnualEnergy;

public sealed class AnnualEnergyBalanceEngine
{
    private const string Method = "Energy Calculation Parity / Annual 8760 Energy Balance";
    private const string Version = "2026.04-internal-deterministic";

    private readonly TimeProvider _timeProvider;

    public AnnualEnergyBalanceEngine(TimeProvider? timeProvider = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public Result<AnnualEnergyBalanceResult> Calculate(AnnualEnergyBalanceInput input)
    {
        if (input is null)
            return Result<AnnualEnergyBalanceResult>.Validation("Annual energy balance input is required.");

        if (input.Hours is null)
            return Result<AnnualEnergyBalanceResult>.Validation("Annual energy balance hourly inputs are required.");

        var diagnostics = Validate(input);
        var assumptions = new List<string>
        {
            "Hourly energy is calculated as sum(W x hourDurationH) / 1000.",
            "Negative hourly heating and cooling loads are clamped to zero and reported in diagnostics.",
            "Annual totals are aggregated from monthly totals."
        };

        if (input.UsesSyntheticWeather)
        {
            diagnostics.Add(new CalculationDiagnostic(
                CalculationDiagnosticSeverity.Warning,
                "AnnualEnergy.SyntheticWeather",
                "Synthetic weather profile was used for annual energy balance.",
                input.DiagnosticsContext));
        }

        var monthly = Enumerable.Range(1, 12)
            .Select(month =>
            {
                var hours = input.Hours.Where(hour => ResolveMonth(hour) == month).ToArray();
                var heating = hours.Sum(hour => Positive(hour.HeatingLoadW, nameof(hour.HeatingLoadW), diagnostics) * hour.HourDurationH) / 1000.0;
                var cooling = hours.Sum(hour => Positive(hour.CoolingLoadW, nameof(hour.CoolingLoadW), diagnostics) * hour.HourDurationH) / 1000.0;
                return new AnnualEnergyMonthlyResult(
                    month,
                    Round(heating),
                    Round(cooling),
                    Round(heating + cooling));
            })
            .ToArray();

        var annualHeating = monthly.Sum(month => month.HeatingKWh);
        var annualCooling = monthly.Sum(month => month.CoolingKWh);
        var annualTotal = annualHeating + annualCooling;
        var peakHeating = input.Hours
            .OrderByDescending(hour => Math.Max(0, hour.HeatingLoadW))
            .ThenBy(hour => hour.HourIndex)
            .FirstOrDefault();
        var peakCooling = input.Hours
            .OrderByDescending(hour => Math.Max(0, hour.CoolingLoadW))
            .ThenBy(hour => hour.HourIndex)
            .FirstOrDefault();
        var componentBreakdown = new AnnualEnergyComponentBreakdown(
            TransmissionKWh: Round(input.Hours.Sum(hour => Positive(hour.TransmissionW, nameof(hour.TransmissionW), diagnostics) * hour.HourDurationH) / 1000.0),
            VentilationKWh: Round(input.Hours.Sum(hour => Positive(hour.VentilationW, nameof(hour.VentilationW), diagnostics) * hour.HourDurationH) / 1000.0),
            InfiltrationKWh: Round(input.Hours.Sum(hour => Positive(hour.InfiltrationW, nameof(hour.InfiltrationW), diagnostics) * hour.HourDurationH) / 1000.0),
            SolarGainsKWh: Round(input.Hours.Sum(hour => Positive(hour.SolarGainsW, nameof(hour.SolarGainsW), diagnostics) * hour.HourDurationH) / 1000.0),
            InternalGainsKWh: Round(input.Hours.Sum(hour => Positive(hour.InternalGainsW, nameof(hour.InternalGainsW), diagnostics) * hour.HourDurationH) / 1000.0),
            GroundKWh: Round(input.Hours.Sum(hour => Positive(hour.GroundW, nameof(hour.GroundW), diagnostics) * hour.HourDurationH) / 1000.0));

        return Result<AnnualEnergyBalanceResult>.Success(new AnnualEnergyBalanceResult(
            input.BuildingId,
            input.BuildingName,
            input.Year,
            Round(annualHeating),
            Round(annualCooling),
            Round(annualTotal),
            input.BuildingAreaM2 > 0 ? Round(annualTotal / input.BuildingAreaM2) : 0,
            monthly,
            peakHeating is null ? 0 : Round(Math.Max(0, peakHeating.HeatingLoadW)),
            peakCooling is null ? 0 : Round(Math.Max(0, peakCooling.CoolingLoadW)),
            peakHeating?.HourIndex,
            peakCooling?.HourIndex,
            componentBreakdown,
            diagnostics,
            assumptions,
            Method,
            Version,
            _timeProvider.GetUtcNow()));
    }

    private static List<CalculationDiagnostic> Validate(AnnualEnergyBalanceInput input)
    {
        var diagnostics = new List<CalculationDiagnostic>();

        if (input.BuildingId < 0)
            diagnostics.Add(Error("AnnualEnergy.InvalidBuildingId", "Building id must not be negative.", input.DiagnosticsContext));

        if (input.BuildingAreaM2 <= 0)
            diagnostics.Add(Error("AnnualEnergy.InvalidArea", "Building area must be greater than zero for EUI calculation.", input.DiagnosticsContext));

        if (input.Hours.Count == 0)
            diagnostics.Add(Error("AnnualEnergy.NoHourlyInputs", "At least one hourly energy balance input is required.", input.DiagnosticsContext));

        if (input.Hours.Count is not 0 and not 8760)
        {
            diagnostics.Add(new CalculationDiagnostic(
                CalculationDiagnosticSeverity.Warning,
                "AnnualEnergy.Not8760",
                "Hourly input count is not 8760; annual totals use the supplied calculation period.",
                input.DiagnosticsContext));
        }

        foreach (var hour in input.Hours)
        {
            if (hour.HourDurationH <= 0)
                diagnostics.Add(Error("AnnualEnergy.InvalidHourDuration", "Hour duration must be greater than zero.", input.DiagnosticsContext));

            if (ResolveMonth(hour) is < 1 or > 12)
                diagnostics.Add(Error("AnnualEnergy.InvalidMonth", "Month must be between 1 and 12.", input.DiagnosticsContext));
        }

        return diagnostics;
    }

    private static int ResolveMonth(AnnualEnergyBalanceHourInput hour)
    {
        if (hour.Month is >= 1 and <= 12)
            return hour.Month;

        var dayOfYear = Math.Clamp(hour.HourIndex, 0, 8759) / 24;
        var daysPerMonth = new[] { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };
        var accumulated = 0;
        for (var month = 1; month <= 12; month++)
        {
            accumulated += daysPerMonth[month - 1];
            if (dayOfYear < accumulated)
                return month;
        }

        return 12;
    }

    private static double Positive(double value, string component, ICollection<CalculationDiagnostic> diagnostics)
    {
        if (value >= 0)
            return value;

        diagnostics.Add(new CalculationDiagnostic(
            CalculationDiagnosticSeverity.Warning,
            "AnnualEnergy.NegativeHourlyValueClamped",
            $"Hourly component {component} was negative and was clamped to zero."));
        return 0;
    }

    private static CalculationDiagnostic Error(string code, string message, string? context) =>
        new(CalculationDiagnosticSeverity.Error, code, message, context);

    private static double Round(double value) =>
        Math.Round(value, 6, MidpointRounding.AwayFromZero);
}
