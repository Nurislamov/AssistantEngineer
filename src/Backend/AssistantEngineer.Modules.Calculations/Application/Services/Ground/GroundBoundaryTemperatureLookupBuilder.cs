using AssistantEngineer.Modules.Calculations.Application.Abstractions.Ground;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Ground;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Ground;

public sealed class GroundBoundaryTemperatureLookupBuilder : IGroundBoundaryTemperatureLookupBuilder
{
    public GroundBoundaryTemperatureLookup Build(BuildingGroundBoundaryCalculationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        var diagnostics = new List<StandardCalculationDiagnostic>();
        diagnostics.AddRange(result.Diagnostics);

        var hourlyBySurfaceId = new Dictionary<string, IReadOnlyList<double>>(StringComparer.Ordinal);
        var monthlyBySurfaceId = new Dictionary<string, IReadOnlyList<double>>(StringComparer.Ordinal);
        var representativeBySurfaceId = new Dictionary<string, double>(StringComparer.Ordinal);

        foreach (var surfaceResult in result.GroundSurfaces)
        {
            var surfaceId = surfaceResult.SurfaceId;
            if (string.IsNullOrWhiteSpace(surfaceId))
            {
                continue;
            }

            var hasValidHourly = false;
            var hourly = surfaceResult.HourlyGroundBoundaryTemperaturesCelsius;
            if (hourly.Count > 0)
            {
                if (hourly.Count == 8760 && hourly.All(double.IsFinite))
                {
                    hourlyBySurfaceId[surfaceId] = hourly;
                    hasValidHourly = true;
                }
                else
                {
                    diagnostics.Add(CreateWarning(
                        "AE-GROUND-HOURLY-LOOKUP-PROFILE-INVALID",
                        $"Surface '{surfaceId}' hourly profile must contain exactly 8760 finite values."));
                }
            }

            var hasValidMonthly = false;
            var monthly = surfaceResult.MonthlyGroundBoundaryTemperaturesCelsius;
            if (monthly.Count > 0)
            {
                if (monthly.Count == 12 && monthly.All(double.IsFinite))
                {
                    monthlyBySurfaceId[surfaceId] = monthly;
                    hasValidMonthly = true;
                }
                else
                {
                    diagnostics.Add(CreateWarning(
                        "AE-GROUND-MONTHLY-LOOKUP-PROFILE-INVALID",
                        $"Surface '{surfaceId}' monthly profile must contain exactly 12 finite values."));
                }
            }

            if (hasValidHourly)
            {
                representativeBySurfaceId[surfaceId] = hourlyBySurfaceId[surfaceId].Average();
            }
            else if (hasValidMonthly)
            {
                representativeBySurfaceId[surfaceId] = monthlyBySurfaceId[surfaceId].Average();
            }
            else
            {
                diagnostics.Add(CreateWarning(
                    "AE-GROUND-REPRESENTATIVE-TEMPERATURE-MISSING",
                    $"Surface '{surfaceId}' has no valid hourly/monthly profile to derive representative temperature."));
            }
        }

        return new GroundBoundaryTemperatureLookup(
            HourlyGroundTemperaturesBySurfaceId: hourlyBySurfaceId,
            MonthlyGroundTemperaturesBySurfaceId: monthlyBySurfaceId,
            RepresentativeGroundTemperatureBySurfaceId: representativeBySurfaceId,
            Diagnostics: diagnostics);
    }

    private static StandardCalculationDiagnostic CreateWarning(
        string code,
        string message) =>
        GroundCalculationDiagnosticsFactory.Create(
            CalculationDiagnosticSeverity.Warning,
            code,
            message,
            StandardCalculationStage.ProfileExpansion,
            "GroundBoundaryTemperatureLookupBuilder");
}
