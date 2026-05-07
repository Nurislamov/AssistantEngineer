using AssistantEngineer.Modules.Calculations.Application.Abstractions.Ground;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Ground;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Physical;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Ground;

public sealed class GroundBoundaryToIso52016BoundaryProfileMapper : IGroundBoundaryToIso52016BoundaryProfileMapper
{
    public GroundBoundaryIso52016BoundaryProfileMappingResult Map(
        GroundBoundaryTemperatureLookup lookup)
    {
        ArgumentNullException.ThrowIfNull(lookup);

        var diagnostics = new List<StandardCalculationDiagnostic>();
        diagnostics.AddRange(lookup.Diagnostics);

        var boundaryConditions = new List<Iso52016PhysicalSurfaceHourlyBoundaryCondition>();

        foreach (var pair in lookup.HourlyGroundTemperaturesBySurfaceId)
        {
            var surfaceId = pair.Key;
            var hourly = pair.Value;

            if (string.IsNullOrWhiteSpace(surfaceId))
            {
                diagnostics.Add(CreateWarning(
                    "AE-GROUND-ISO52016-SURFACE-ID-MISSING",
                    "Ground lookup contains an empty surface id and cannot be mapped to ISO52016 physical boundary conditions."));
                continue;
            }

            if (hourly.Count != 8760 || hourly.Any(value => !double.IsFinite(value)))
            {
                diagnostics.Add(CreateWarning(
                    "AE-GROUND-ISO52016-HOURLY-PROFILE-INVALID",
                    $"Surface '{surfaceId}' must provide 8760 finite hourly ground temperatures for ISO52016 physical boundary mapping."));
                continue;
            }

            for (var hourOfYear = 0; hourOfYear < hourly.Count; hourOfYear++)
            {
                boundaryConditions.Add(new Iso52016PhysicalSurfaceHourlyBoundaryCondition(
                    SurfaceId: surfaceId,
                    HourOfYear: hourOfYear,
                    BoundaryTemperatureC: hourly[hourOfYear]));
            }
        }

        return new GroundBoundaryIso52016BoundaryProfileMappingResult(
            SurfaceBoundaryConditions: boundaryConditions,
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
            "GroundBoundaryToIso52016BoundaryProfileMapper");
}
