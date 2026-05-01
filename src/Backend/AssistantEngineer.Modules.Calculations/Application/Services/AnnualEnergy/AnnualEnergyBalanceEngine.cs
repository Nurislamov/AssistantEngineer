using AssistantEngineer.Modules.Calculations.Application.Contracts.AnnualEnergy;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Services.AnnualEnergy;

public sealed class AnnualEnergyBalanceEngine
{
    private const string Method = "Energy Calculation Parity / Annual 8760 Energy Balance";
    private const string Version = "2026.04-internal-deterministic";
    private const string TrueHourlySimulationSource = "TrueHourlySimulation";
    private const string MonthlyBalanceAdapterSource = "MonthlyBalanceAdapter";
    private const string DeterministicFixtureSource = "DeterministicFixture";
    private const string UnavailableSource = "Unavailable";

    private readonly TimeProvider _timeProvider;

    public AnnualEnergyBalanceEngine(TimeProvider? timeProvider = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public Result<AnnualEnergyBalanceResult> Calculate(
        AnnualEnergyBalanceInput input)
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
            "Magnitude component totals are calculated from non-negative component values.",
            "Net component totals preserve sign: positive means heat gain to the building, negative means heat loss from the building.",
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

        var energyDataSource = ResolveEnergyDataSource(input);
        var hourlyRecordCount = input.Hours.Count;
        var isTrueHourly8760 =
            energyDataSource == TrueHourlySimulationSource &&
            input.IsTrueHourly8760 &&
            hourlyRecordCount == 8760;

        AddSourceDiagnostics(
            diagnostics,
            energyDataSource,
            isTrueHourly8760,
            hourlyRecordCount,
            input.DiagnosticsContext);

        AddSignedBalanceDiagnostics(
            diagnostics,
            input.Hours,
            input.DiagnosticsContext);

        var monthly = Enumerable.Range(1, 12)
            .Select(month =>
            {
                var hours = input.Hours
                    .Where(hour => ResolveMonth(hour) == month)
                    .ToArray();

                var heating = hours.Sum(hour =>
                    Positive(hour.HeatingLoadW, nameof(hour.HeatingLoadW), diagnostics) *
                    hour.HourDurationH) / 1000.0;

                var cooling = hours.Sum(hour =>
                    Positive(hour.CoolingLoadW, nameof(hour.CoolingLoadW), diagnostics) *
                    hour.HourDurationH) / 1000.0;

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

        var mechanicalVentilationKWh = Round(input.Hours.Sum(hour =>
            Positive(hour.MechanicalVentilationW, nameof(hour.MechanicalVentilationW), diagnostics) *
            hour.HourDurationH) / 1000.0);

        var naturalVentilationKWh = Round(input.Hours.Sum(hour =>
            Positive(hour.NaturalVentilationW, nameof(hour.NaturalVentilationW), diagnostics) *
            hour.HourDurationH) / 1000.0);

        var netMechanicalVentilationKWh = Round(input.Hours.Sum(hour =>
            hour.MechanicalVentilationBalanceW * hour.HourDurationH) / 1000.0);

        var netNaturalVentilationKWh = Round(input.Hours.Sum(hour =>
            hour.NaturalVentilationBalanceW * hour.HourDurationH) / 1000.0);

        var hasVentilationSubcomponentSplit =
            input.Hours.Any(hour =>
                Math.Abs(hour.MechanicalVentilationW) > 0.000001 ||
                Math.Abs(hour.NaturalVentilationW) > 0.000001 ||
                Math.Abs(hour.MechanicalVentilationBalanceW) > 0.000001 ||
                Math.Abs(hour.NaturalVentilationBalanceW) > 0.000001);

        var componentBreakdown = new AnnualEnergyComponentBreakdown(
            TransmissionKWh: Round(input.Hours.Sum(hour =>
                Positive(hour.TransmissionW, nameof(hour.TransmissionW), diagnostics) *
                hour.HourDurationH) / 1000.0),

            VentilationKWh: hasVentilationSubcomponentSplit
                ? Round(mechanicalVentilationKWh + naturalVentilationKWh)
                : Round(input.Hours.Sum(hour =>
                    Positive(hour.VentilationW, nameof(hour.VentilationW), diagnostics) *
                    hour.HourDurationH) / 1000.0),

            InfiltrationKWh: Round(input.Hours.Sum(hour =>
                Positive(hour.InfiltrationW, nameof(hour.InfiltrationW), diagnostics) *
                hour.HourDurationH) / 1000.0),

            SolarGainsKWh: Round(input.Hours.Sum(hour =>
                Positive(hour.SolarGainsW, nameof(hour.SolarGainsW), diagnostics) *
                hour.HourDurationH) / 1000.0),

            InternalGainsKWh: Round(input.Hours.Sum(hour =>
                Positive(hour.InternalGainsW, nameof(hour.InternalGainsW), diagnostics) *
                hour.HourDurationH) / 1000.0),

            GroundKWh: Round(input.Hours.Sum(hour =>
                Positive(hour.GroundW, nameof(hour.GroundW), diagnostics) *
                hour.HourDurationH) / 1000.0),

            NetTransmissionKWh: Round(input.Hours.Sum(hour =>
                hour.TransmissionBalanceW * hour.HourDurationH) / 1000.0),

            NetVentilationKWh: hasVentilationSubcomponentSplit
                ? Round(netMechanicalVentilationKWh + netNaturalVentilationKWh)
                : Round(input.Hours.Sum(hour =>
                    hour.VentilationBalanceW * hour.HourDurationH) / 1000.0),

            NetInfiltrationKWh: Round(input.Hours.Sum(hour =>
                hour.InfiltrationBalanceW * hour.HourDurationH) / 1000.0),

            NetGroundKWh: Round(input.Hours.Sum(hour =>
                hour.GroundBalanceW * hour.HourDurationH) / 1000.0),

            MechanicalVentilationKWh: mechanicalVentilationKWh,

            NaturalVentilationKWh: naturalVentilationKWh,

            NetMechanicalVentilationKWh: netMechanicalVentilationKWh,

            NetNaturalVentilationKWh: netNaturalVentilationKWh);

        return Result<AnnualEnergyBalanceResult>.Success(
            new AnnualEnergyBalanceResult(
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
                _timeProvider.GetUtcNow(),
                energyDataSource,
                isTrueHourly8760,
                hourlyRecordCount,
                input.ActualMethod ?? Method));
    }

    private static List<CalculationDiagnostic> Validate(
        AnnualEnergyBalanceInput input)
    {
        var diagnostics = new List<CalculationDiagnostic>();

        if (input.BuildingId < 0)
        {
            diagnostics.Add(Error(
                "AnnualEnergy.InvalidBuildingId",
                "Building id must not be negative.",
                input.DiagnosticsContext));
        }

        if (input.BuildingAreaM2 <= 0)
        {
            diagnostics.Add(Error(
                "AnnualEnergy.InvalidArea",
                "Building area must be greater than zero for EUI calculation.",
                input.DiagnosticsContext));
        }

        if (input.Hours.Count == 0)
        {
            diagnostics.Add(Error(
                "AnnualEnergy.NoHourlyInputs",
                "At least one hourly energy balance input is required.",
                input.DiagnosticsContext));
        }

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
            {
                diagnostics.Add(Error(
                    "AnnualEnergy.InvalidHourDuration",
                    "Hour duration must be greater than zero.",
                    input.DiagnosticsContext));
            }

            if (ResolveMonth(hour) is < 1 or > 12)
            {
                diagnostics.Add(Error(
                    "AnnualEnergy.InvalidMonth",
                    "Month must be between 1 and 12.",
                    input.DiagnosticsContext));
            }
        }

        return diagnostics;
    }

    private static int ResolveMonth(
        AnnualEnergyBalanceHourInput hour)
    {
        if (hour.Month is >= 1 and <= 12)
            return hour.Month;

        var dayOfYear = Math.Clamp(hour.HourIndex, 0, 8759) / 24;
        var daysPerMonth = new[]
        {
            31,
            28,
            31,
            30,
            31,
            30,
            31,
            31,
            30,
            31,
            30,
            31
        };

        var accumulated = 0;

        for (var month = 1; month <= 12; month++)
        {
            accumulated += daysPerMonth[month - 1];

            if (dayOfYear < accumulated)
                return month;
        }

        return 12;
    }

    private static string ResolveEnergyDataSource(
        AnnualEnergyBalanceInput input)
    {
        if (!string.IsNullOrWhiteSpace(input.EnergyDataSource))
            return input.EnergyDataSource;

        if (input.UsesSyntheticWeather)
            return MonthlyBalanceAdapterSource;

        return input.Hours.Count == 0
            ? UnavailableSource
            : DeterministicFixtureSource;
    }

    private static void AddSourceDiagnostics(
        ICollection<CalculationDiagnostic> diagnostics,
        string energyDataSource,
        bool isTrueHourly8760,
        int hourlyRecordCount,
        string? context)
    {
        switch (energyDataSource)
        {
            case TrueHourlySimulationSource when isTrueHourly8760:
                diagnostics.Add(new CalculationDiagnostic(
                    CalculationDiagnosticSeverity.Info,
                    "AnnualEnergy.TrueHourlySimulationUsed",
                    "Annual energy balance was calculated from true hourly simulation records.",
                    context));
                break;

            case TrueHourlySimulationSource:
                diagnostics.Add(new CalculationDiagnostic(
                    CalculationDiagnosticSeverity.Warning,
                    "AnnualEnergy.TrueHourlySimulationPartial",
                    $"Annual energy balance was calculated from hourly simulation records, but the record count is {hourlyRecordCount}; this is not a true hourly 8760 simulation.",
                    context));
                break;

            case MonthlyBalanceAdapterSource:
                diagnostics.Add(new CalculationDiagnostic(
                    CalculationDiagnosticSeverity.Warning,
                    "AnnualEnergy.MonthlyBalanceAdapter",
                    "Annual energy balance uses representative monthly records generated from monthly balances; this is not a true hourly 8760 simulation.",
                    context));
                break;

            case UnavailableSource:
                diagnostics.Add(new CalculationDiagnostic(
                    CalculationDiagnosticSeverity.Error,
                    "AnnualEnergy.SourceUnavailable",
                    "Annual energy balance source data is unavailable.",
                    context));
                break;
        }
    }

    private static void AddSignedBalanceDiagnostics(
        ICollection<CalculationDiagnostic> diagnostics,
        IReadOnlyList<AnnualEnergyBalanceHourInput> hours,
        string? context)
    {
        if (hours.Count == 0)
            return;

        var hasSignedBalance =
            hours.Any(hour =>
                Math.Abs(hour.TransmissionBalanceW) > 0.000001 ||
                Math.Abs(hour.VentilationBalanceW) > 0.000001 ||
                Math.Abs(hour.MechanicalVentilationBalanceW) > 0.000001 ||
                Math.Abs(hour.NaturalVentilationBalanceW) > 0.000001 ||
                Math.Abs(hour.InfiltrationBalanceW) > 0.000001 ||
                Math.Abs(hour.GroundBalanceW) > 0.000001);

        if (!hasSignedBalance)
            return;

        diagnostics.Add(new CalculationDiagnostic(
            CalculationDiagnosticSeverity.Info,
            "AnnualEnergy.SignedComponentBalanceAvailable",
            "Annual energy balance contains signed component totals. Positive net values mean heat gain to the building; negative net values mean heat loss from the building.",
            context));
    }

    private static double Positive(
        double value,
        string component,
        ICollection<CalculationDiagnostic> diagnostics)
    {
        if (value >= 0)
            return value;

        diagnostics.Add(new CalculationDiagnostic(
            CalculationDiagnosticSeverity.Warning,
            "AnnualEnergy.NegativeHourlyValueClamped",
            $"Hourly component {component} was negative and was clamped to zero."));

        return 0;
    }

    private static CalculationDiagnostic Error(
        string code,
        string message,
        string? context) =>
        new(
            CalculationDiagnosticSeverity.Error,
            code,
            message,
            context);

    private static double Round(
        double value) =>
        Math.Round(value, 6, MidpointRounding.AwayFromZero);
}
