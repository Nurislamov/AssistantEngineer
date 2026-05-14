using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Topology;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Ventilation;

internal static class NaturalVentilationZoneInputNormalizer
{
    private const double DefaultAirSpecificHeatJPerKgKelvin = 1005.0;
    private const double DefaultAirDensityKgPerCubicMeter = 1.204;

    public static IReadOnlyList<NaturalVentilationHourlyControlContext> BuildControlContexts(
        IEnumerable<NaturalVentilationHourlyZoneEnvironment> environments) =>
        environments
            .Select(environment => new NaturalVentilationHourlyControlContext(
                HourIndex: environment.HourIndex,
                IndoorTemperatureCelsius: environment.IndoorTemperatureCelsius,
                OutdoorTemperatureCelsius: environment.OutdoorTemperatureCelsius,
                WindSpeedMetersPerSecond: environment.WindSpeedMetersPerSecond,
                OccupancyFraction: environment.OccupancyFraction,
                ScheduleFraction: environment.ScheduleFraction,
                IsNightHour: environment.IsNightHour,
                RoomId: environment.RoomId,
                ZoneId: environment.ZoneId,
                Diagnostics: environment.Diagnostics,
                HeatingModeActive: environment.HeatingModeActive,
                CoolingModeActive: environment.CoolingModeActive))
            .ToArray();

    public static bool MatchesEnvironment(
        NaturalVentilationOpeningOperationResult operation,
        NaturalVentilationHourlyZoneEnvironment environment)
    {
        if (!string.IsNullOrWhiteSpace(environment.RoomId))
        {
            if (!string.IsNullOrWhiteSpace(operation.RoomId) &&
                !string.Equals(operation.RoomId, environment.RoomId, StringComparison.Ordinal))
            {
                return false;
            }
        }
        else if (!string.IsNullOrWhiteSpace(operation.RoomId))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(environment.ZoneId))
        {
            if (!string.IsNullOrWhiteSpace(operation.ZoneId) &&
                !string.Equals(operation.ZoneId, environment.ZoneId, StringComparison.Ordinal))
            {
                return false;
            }
        }
        else if (!string.IsNullOrWhiteSpace(operation.ZoneId))
        {
            return false;
        }

        return true;
    }

    public static double ResolveAirSpecificHeat(
        NaturalVentilationZoneIntegrationInput input,
        NaturalVentilationHourlyZoneEnvironment environment,
        ICollection<StandardCalculationDiagnostic> diagnostics,
        int hourIndex)
    {
        if (environment.AirSpecificHeatJPerKgKelvin is > 0.0)
            return environment.AirSpecificHeatJPerKgKelvin.Value;

        if (input.DefaultAirSpecificHeatJPerKgKelvin is > 0.0)
            return input.DefaultAirSpecificHeatJPerKgKelvin.Value;

        diagnostics.Add(NaturalVentilationZoneDiagnosticsBuilder.Info(
            "AE-VENT-ZONE-AIR-CP-DEFAULTED",
            $"Air specific heat defaulted to {DefaultAirSpecificHeatJPerKgKelvin:F1} J/(kg.K) at hour {hourIndex}."));
        return DefaultAirSpecificHeatJPerKgKelvin;
    }

    public static double ResolveAirDensity(
        NaturalVentilationZoneIntegrationInput input,
        NaturalVentilationHourlyZoneEnvironment environment,
        ICollection<StandardCalculationDiagnostic> diagnostics,
        int hourIndex)
    {
        if (environment.AirDensityKgPerCubicMeter is > 0.0)
            return environment.AirDensityKgPerCubicMeter.Value;

        if (input.DefaultAirDensityKgPerCubicMeter is > 0.0)
            return input.DefaultAirDensityKgPerCubicMeter.Value;

        diagnostics.Add(NaturalVentilationZoneDiagnosticsBuilder.Info(
            "AE-VENT-ZONE-AIR-DENSITY-DEFAULTED",
            $"Air density defaulted to {DefaultAirDensityKgPerCubicMeter:F3} kg/m3 at hour {hourIndex}."));
        return DefaultAirDensityKgPerCubicMeter;
    }
}
