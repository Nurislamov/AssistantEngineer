using AssistantEngineer.Modules.Calculations.Application.Abstractions.Topology;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Topology;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Topology;

public sealed class ThermalTopologyValidator : IThermalTopologyValidator
{
    private readonly IThermalBoundaryConditionResolver _boundaryConditionResolver;

    public ThermalTopologyValidator(
        IThermalBoundaryConditionResolver boundaryConditionResolver)
    {
        _boundaryConditionResolver = boundaryConditionResolver ?? throw new ArgumentNullException(nameof(boundaryConditionResolver));
    }

    public ThermalTopologyValidationResult Validate(BuildingThermalTopology topology)
    {
        ArgumentNullException.ThrowIfNull(topology);

        var diagnostics = new List<StandardCalculationDiagnostic>();

        if (string.IsNullOrWhiteSpace(topology.BuildingId))
        {
            diagnostics.Add(CreateError(
                "Topology.Validator.BuildingIdMissing",
                "BuildingId is required."));
        }

        ValidateUniqueIds(topology.Zones.Select(zone => zone.ZoneId), "Zone", "ZoneId", diagnostics);
        ValidateUniqueIds(topology.Rooms.Select(room => room.RoomId), "Room", "RoomId", diagnostics);
        ValidateUniqueIds(topology.Surfaces.Select(surface => surface.SurfaceId), "Surface", "SurfaceId", diagnostics);

        var zoneIds = topology.Zones
            .Select(zone => zone.ZoneId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet(StringComparer.Ordinal);
        var roomIds = topology.Rooms
            .Select(room => room.RoomId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet(StringComparer.Ordinal);

        foreach (var room in topology.Rooms)
        {
            if (!string.IsNullOrWhiteSpace(room.ZoneId) && !zoneIds.Contains(room.ZoneId))
            {
                diagnostics.Add(CreateError(
                    "Topology.Validator.RoomZoneMissing",
                    $"Room '{room.RoomId}' references zone '{room.ZoneId}' that is not present in topology."));
            }
        }

        foreach (var zone in topology.Zones)
        {
            foreach (var roomId in zone.RoomIds.Where(roomId => !string.IsNullOrWhiteSpace(roomId)))
            {
                if (!roomIds.Contains(roomId))
                {
                    diagnostics.Add(CreateError(
                        "Topology.Validator.ZoneRoomMissing",
                        $"Zone '{zone.ZoneId}' references room '{roomId}' that is not present in topology."));
                }
            }
        }

        foreach (var surface in topology.Surfaces)
        {
            if (!string.IsNullOrWhiteSpace(surface.RoomId) && !roomIds.Contains(surface.RoomId))
            {
                diagnostics.Add(CreateError(
                    "Topology.Validator.SurfaceRoomMissing",
                    $"Surface '{surface.SurfaceId}' references room '{surface.RoomId}' that is not present in topology."));
            }

            if (!string.IsNullOrWhiteSpace(surface.ZoneId) && !zoneIds.Contains(surface.ZoneId))
            {
                diagnostics.Add(CreateError(
                    "Topology.Validator.SurfaceZoneMissing",
                    $"Surface '{surface.SurfaceId}' references zone '{surface.ZoneId}' that is not present in topology."));
            }

            if (!(surface.AreaSquareMeters > 0.0))
            {
                diagnostics.Add(CreateError(
                    "Topology.Validator.SurfaceAreaNonPositive",
                    $"Surface '{surface.SurfaceId}' area must be greater than zero."));
            }

            if (surface.UValueWPerSquareMeterKelvin.HasValue &&
                !(surface.UValueWPerSquareMeterKelvin.Value > 0.0))
            {
                diagnostics.Add(CreateError(
                    "Topology.Validator.SurfaceUValueNonPositive",
                    $"Surface '{surface.SurfaceId}' U-value must be greater than zero when provided."));
            }

            var resolution = _boundaryConditionResolver.Resolve(surface, topology);
            diagnostics.AddRange(resolution.Diagnostics);
            ValidateBoundaryResolution(surface, resolution, diagnostics);
        }

        return new ThermalTopologyValidationResult(
            IsValid: diagnostics.Count == 0,
            Diagnostics: diagnostics);
    }

    private static void ValidateUniqueIds(
        IEnumerable<string> ids,
        string entityLabel,
        string idLabel,
        ICollection<StandardCalculationDiagnostic> diagnostics)
    {
        foreach (var duplicateId in ids
                     .Where(id => !string.IsNullOrWhiteSpace(id))
                     .GroupBy(id => id, StringComparer.Ordinal)
                     .Where(group => group.Count() > 1)
                     .Select(group => group.Key))
        {
            diagnostics.Add(CreateError(
                $"Topology.Validator.Duplicate{idLabel}",
                $"{entityLabel} identifier '{duplicateId}' is duplicated."));
        }
    }

    private static void ValidateBoundaryResolution(
        ThermalTopologySurface surface,
        ThermalBoundaryResolutionResult resolution,
        ICollection<StandardCalculationDiagnostic> diagnostics)
    {
        switch (surface.BoundaryKind)
        {
            case ThermalBoundaryKind.Outdoor:
                if (!resolution.IsHeatTransferBoundary || !resolution.RequiresOutdoorTemperature)
                {
                    diagnostics.Add(CreateError(
                        "Topology.Validator.OutdoorResolutionInvalid",
                        $"Surface '{surface.SurfaceId}' outdoor boundary resolution must require outdoor temperature."));
                }

                break;

            case ThermalBoundaryKind.Ground:
                if (!resolution.IsHeatTransferBoundary || !resolution.RequiresGroundTemperature)
                {
                    diagnostics.Add(CreateError(
                        "Topology.Validator.GroundResolutionInvalid",
                        $"Surface '{surface.SurfaceId}' ground boundary resolution must require ground temperature."));
                }

                break;

            case ThermalBoundaryKind.AdjacentConditionedZone:
                if (!resolution.RequiresAdjacentZoneTemperature)
                {
                    diagnostics.Add(CreateError(
                        "Topology.Validator.AdjacentConditionedResolutionInvalid",
                        $"Surface '{surface.SurfaceId}' adjacent-conditioned boundary must require adjacent zone temperature."));
                }

                break;

            case ThermalBoundaryKind.AdjacentUnconditionedZone:
                if (!resolution.RequiresAdjacentUnconditionedTemperature)
                {
                    diagnostics.Add(CreateError(
                        "Topology.Validator.AdjacentUnconditionedResolutionInvalid",
                        $"Surface '{surface.SurfaceId}' adjacent-unconditioned boundary must require adjacent unconditioned temperature."));
                }

                break;

            case ThermalBoundaryKind.Adiabatic:
                if (!resolution.IsAdiabatic ||
                    resolution.RequiresOutdoorTemperature ||
                    resolution.RequiresGroundTemperature ||
                    resolution.RequiresAdjacentZoneTemperature ||
                    resolution.RequiresAdjacentUnconditionedTemperature)
                {
                    diagnostics.Add(CreateError(
                        "Topology.Validator.AdiabaticResolutionInvalid",
                        $"Surface '{surface.SurfaceId}' adiabatic boundary must not require external temperatures."));
                }

                break;

            case ThermalBoundaryKind.InternalPartition:
                if (resolution.RequiresOutdoorTemperature || resolution.RequiresGroundTemperature)
                {
                    diagnostics.Add(CreateError(
                        "Topology.Validator.InternalPartitionExternalTemperatureInvalid",
                        $"Surface '{surface.SurfaceId}' internal partition cannot resolve to outdoor/ground requirements."));
                }

                break;
        }
    }

    private static StandardCalculationDiagnostic CreateError(
        string code,
        string message) =>
        ThermalTopologyDiagnosticsFactory.Create(
            severity: CalculationDiagnosticSeverity.Error,
            code: code,
            message: message,
            context: "ThermalTopologyValidator",
            stage: StandardCalculationStage.Diagnostics);
}
