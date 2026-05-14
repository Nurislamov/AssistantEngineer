using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Ventilation;

internal static class VentilationInputNormalizer
{
    private const double MinimumTemperatureC = -100.0;
    private const double MaximumTemperatureC = 100.0;

    public static List<CalculationDiagnostic> Validate(
        VentilationAndInfiltrationLoadInput input)
    {
        var diagnostics = new List<CalculationDiagnostic>();

        if (input.AreaM2 <= 0)
        {
            diagnostics.Add(VentilationDiagnosticsBuilder.Error(
                "Ventilation.InvalidArea",
                "Room area must be greater than zero.",
                input.DiagnosticsContext));
        }

        if (input.VolumeM3 <= 0)
        {
            diagnostics.Add(VentilationDiagnosticsBuilder.Error(
                "Ventilation.InvalidVolume",
                "Room volume must be greater than zero.",
                input.DiagnosticsContext));
        }

        if (input.OccupancyPeople < 0)
        {
            diagnostics.Add(VentilationDiagnosticsBuilder.Error(
                "Ventilation.InvalidOccupancy",
                "People count cannot be negative.",
                input.DiagnosticsContext));
        }

        if (input.IndoorTemperatureC is < MinimumTemperatureC or > MaximumTemperatureC)
        {
            diagnostics.Add(VentilationDiagnosticsBuilder.Error(
                "Ventilation.InvalidIndoorTemperature",
                "Indoor temperature is outside the supported calculation range.",
                input.DiagnosticsContext));
        }

        if (input.OutdoorTemperatureC is < MinimumTemperatureC or > MaximumTemperatureC)
        {
            diagnostics.Add(VentilationDiagnosticsBuilder.Error(
                "Ventilation.InvalidOutdoorTemperature",
                "Outdoor temperature is outside the supported calculation range.",
                input.DiagnosticsContext));
        }

        if (input.HeatRecoveryEfficiency is < 0.0 or > 1.0)
        {
            diagnostics.Add(VentilationDiagnosticsBuilder.Error(
                "Ventilation.InvalidHeatRecoveryEfficiency",
                "Heat recovery efficiency must be between 0 and 1.",
                input.DiagnosticsContext));
        }

        if (input.ScheduleFactor is < 0.0 or > 1.0)
        {
            diagnostics.Add(VentilationDiagnosticsBuilder.Error(
                "Ventilation.InvalidScheduleFactor",
                "Schedule factor must be between 0 and 1.",
                input.DiagnosticsContext));
        }

        if (input.AirDensityKgPerM3 is <= 0)
        {
            diagnostics.Add(VentilationDiagnosticsBuilder.Error(
                "Ventilation.InvalidAirDensity",
                "Air density must be greater than zero.",
                input.DiagnosticsContext));
        }

        if (input.AirSpecificHeatJPerKgK is <= 0)
        {
            diagnostics.Add(VentilationDiagnosticsBuilder.Error(
                "Ventilation.InvalidAirSpecificHeat",
                "Air specific heat must be greater than zero.",
                input.DiagnosticsContext));
        }

        return diagnostics;
    }

    public static VentilationInputNormalization ResolveAirConstants(
        VentilationAndInfiltrationLoadInput input,
        ICollection<CalculationDiagnostic> diagnostics)
    {
        var airDensity = input.AirDensityKgPerM3 ?? AirPhysicalConstants.AirDensityKgPerM3;
        var airSpecificHeat = input.AirSpecificHeatJPerKgK ?? AirPhysicalConstants.AirSpecificHeatJPerKgK;

        if (!input.AirDensityKgPerM3.HasValue ||
            !input.AirSpecificHeatJPerKgK.HasValue)
        {
            diagnostics.Add(VentilationDiagnosticsBuilder.AirConstantsUsed(
                airDensity,
                airSpecificHeat,
                input.DiagnosticsContext));
        }

        return new VentilationInputNormalization(airDensity, airSpecificHeat);
    }

    public static VentilationAirflowResult CreateAirflowResult(
        double roomVolumeM3,
        double airflowM3PerHour)
    {
        var airflowM3PerSecond = AirflowNormalizer.M3PerHourToM3PerSecond(
            Math.Max(airflowM3PerHour, 0.0)).Value;

        var airChangesPerHour = roomVolumeM3 > 0
            ? airflowM3PerHour / roomVolumeM3
            : 0.0;

        return new VentilationAirflowResult(
            airflowM3PerHour,
            airflowM3PerSecond,
            airChangesPerHour);
    }
}
