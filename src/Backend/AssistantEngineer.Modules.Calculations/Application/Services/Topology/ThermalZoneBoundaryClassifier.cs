using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Topology;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Topology;

internal sealed record ThermalZoneSurfaceClassification(
    bool IsResolved,
    string? EffectiveZoneId,
    double? SourceZoneTemperatureCelsius,
    double? AdjacentTemperatureCelsius,
    double? BoundaryTemperatureCelsius,
    IReadOnlyList<StandardCalculationDiagnostic> Diagnostics);

internal static class ThermalZoneBoundaryClassifier
{
    public static ThermalZoneSurfaceClassification Classify(
        ThermalTopologySurface surface,
        IReadOnlyDictionary<string, ThermalTopologyRoom> roomsById,
        IReadOnlyDictionary<string, double> zoneTemperatures,
        IReadOnlyDictionary<string, double> adjacentUnconditionedTemperatures,
        double? outdoorTemperatureCelsius,
        double? groundTemperatureCelsius,
        bool isResolved)
    {
        var diagnostics = new List<StandardCalculationDiagnostic>();
        var sourceZoneId = ThermalZoneAdjacentBoundaryResolver.ResolveSourceZoneId(surface, roomsById);
        var sourceZoneTemperature = ThermalZoneAdjacentBoundaryResolver.TryGetTemperature(sourceZoneId, zoneTemperatures);

        double? boundaryTemperature = null;
        double? adjacentTemperature = null;

        switch (surface.BoundaryKind)
        {
            case ThermalBoundaryKind.Outdoor:
                if (outdoorTemperatureCelsius.HasValue)
                {
                    boundaryTemperature = outdoorTemperatureCelsius.Value;
                }
                else
                {
                    diagnostics.Add(ThermalZoneBoundaryDiagnosticsBuilder.Create(
                        CalculationDiagnosticSeverity.Warning,
                        "AE-ZONES-OUTDOOR-TEMPERATURE-MISSING",
                        $"Surface '{surface.SurfaceId}' requires outdoor temperature but no value was provided.",
                        StandardCalculationStage.BoundaryCondition));
                    isResolved = false;
                }

                break;

            case ThermalBoundaryKind.Ground:
                if (groundTemperatureCelsius.HasValue)
                {
                    boundaryTemperature = groundTemperatureCelsius.Value;
                }
                else
                {
                    diagnostics.Add(ThermalZoneBoundaryDiagnosticsBuilder.Create(
                        CalculationDiagnosticSeverity.Warning,
                        "AE-ZONES-GROUND-TEMPERATURE-MISSING",
                        $"Surface '{surface.SurfaceId}' requires ground temperature but no value was provided.",
                        StandardCalculationStage.BoundaryCondition));
                    isResolved = false;
                }

                break;

            case ThermalBoundaryKind.AdjacentConditionedZone:
                adjacentTemperature = ThermalZoneAdjacentBoundaryResolver.ResolveAdjacentConditionedTemperature(
                    surface,
                    roomsById,
                    zoneTemperatures);
                boundaryTemperature = adjacentTemperature;
                if (!adjacentTemperature.HasValue)
                {
                    diagnostics.Add(ThermalZoneBoundaryDiagnosticsBuilder.Create(
                        CalculationDiagnosticSeverity.Warning,
                        "AE-ZONES-ADJACENT-ZONE-TEMPERATURE-MISSING",
                        $"Surface '{surface.SurfaceId}' adjacent conditioned temperature could not be resolved.",
                        StandardCalculationStage.BoundaryCondition));
                    isResolved = false;
                }

                if (!sourceZoneTemperature.HasValue)
                {
                    diagnostics.Add(ThermalZoneBoundaryDiagnosticsBuilder.Create(
                        CalculationDiagnosticSeverity.Warning,
                        "AE-ZONES-SOURCE-ZONE-TEMPERATURE-MISSING",
                        $"Surface '{surface.SurfaceId}' has no source zone temperature for adjacent conditioned interpretation.",
                        StandardCalculationStage.BoundaryCondition));
                    isResolved = false;
                }

                break;

            case ThermalBoundaryKind.AdjacentUnconditionedZone:
                adjacentTemperature = ThermalZoneAdjacentBoundaryResolver.ResolveAdjacentUnconditionedTemperature(
                    surface,
                    adjacentUnconditionedTemperatures);
                boundaryTemperature = adjacentTemperature;
                if (!adjacentTemperature.HasValue)
                {
                    diagnostics.Add(ThermalZoneBoundaryDiagnosticsBuilder.Create(
                        CalculationDiagnosticSeverity.Warning,
                        "AE-ZONES-ADJACENT-UNCONDITIONED-TEMPERATURE-MISSING",
                        $"Surface '{surface.SurfaceId}' adjacent unconditioned temperature could not be resolved.",
                        StandardCalculationStage.BoundaryCondition));
                    isResolved = false;
                }

                break;

            case ThermalBoundaryKind.InternalPartition:
                adjacentTemperature = ThermalZoneAdjacentBoundaryResolver.ResolveAdjacentConditionedTemperature(
                    surface,
                    roomsById,
                    zoneTemperatures);
                boundaryTemperature = adjacentTemperature;
                if (!adjacentTemperature.HasValue)
                {
                    diagnostics.Add(ThermalZoneBoundaryDiagnosticsBuilder.Create(
                        CalculationDiagnosticSeverity.Warning,
                        "AE-ZONES-INTERNAL-PARTITION-UNRESOLVED",
                        $"Surface '{surface.SurfaceId}' internal partition temperature could not be resolved from adjacent zone/room.",
                        StandardCalculationStage.BoundaryCondition));
                    isResolved = false;
                }

                if (!sourceZoneTemperature.HasValue)
                {
                    diagnostics.Add(ThermalZoneBoundaryDiagnosticsBuilder.Create(
                        CalculationDiagnosticSeverity.Warning,
                        "AE-ZONES-SOURCE-ZONE-TEMPERATURE-MISSING",
                        $"Surface '{surface.SurfaceId}' has no source zone temperature for internal partition interpretation.",
                        StandardCalculationStage.BoundaryCondition));
                    isResolved = false;
                }

                break;

            case ThermalBoundaryKind.Adiabatic:
                boundaryTemperature = sourceZoneTemperature;
                if (!sourceZoneTemperature.HasValue)
                {
                    diagnostics.Add(ThermalZoneBoundaryDiagnosticsBuilder.Create(
                        CalculationDiagnosticSeverity.Warning,
                        "AE-ZONES-SOURCE-ZONE-TEMPERATURE-MISSING",
                        $"Surface '{surface.SurfaceId}' adiabatic interpretation expects source zone temperature.",
                        StandardCalculationStage.BoundaryCondition));
                }

                break;

            case ThermalBoundaryKind.Other:
                diagnostics.Add(ThermalZoneBoundaryDiagnosticsBuilder.Create(
                    CalculationDiagnosticSeverity.Warning,
                    "AE-ZONES-BOUNDARY-OTHER-UNSUPPORTED",
                    $"Surface '{surface.SurfaceId}' boundary kind Other is unsupported for deterministic boundary calculation.",
                    StandardCalculationStage.BoundaryCondition));
                isResolved = false;
                break;
        }

        return new ThermalZoneSurfaceClassification(
            IsResolved: isResolved,
            EffectiveZoneId: sourceZoneId ?? surface.ZoneId,
            SourceZoneTemperatureCelsius: sourceZoneTemperature,
            AdjacentTemperatureCelsius: adjacentTemperature,
            BoundaryTemperatureCelsius: boundaryTemperature,
            Diagnostics: diagnostics);
    }
}
