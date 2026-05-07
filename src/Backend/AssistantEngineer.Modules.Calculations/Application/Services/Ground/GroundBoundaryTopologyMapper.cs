using AssistantEngineer.Modules.Calculations.Application.Abstractions.Ground;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Ground;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Topology;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Ground;

public sealed class GroundBoundaryTopologyMapper : IGroundBoundaryTopologyMapper
{
    public GroundBoundaryCalculationInput Map(
        BuildingThermalTopology topology,
        ThermalTopologySurface surface,
        GroundSurfaceMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(topology);
        ArgumentNullException.ThrowIfNull(surface);
        ArgumentNullException.ThrowIfNull(metadata);
        ArgumentNullException.ThrowIfNull(metadata.Geometry);
        ArgumentNullException.ThrowIfNull(metadata.Soil);
        ArgumentNullException.ThrowIfNull(metadata.Climate);

        var diagnostics = new List<StandardCalculationDiagnostic>();
        diagnostics.AddRange(metadata.Diagnostics);
        diagnostics.AddRange(metadata.Geometry.Diagnostics);

        if (surface.BoundaryKind != ThermalBoundaryKind.Ground)
        {
            diagnostics.Add(CreateWarning(
                "AE-GROUND-SURFACE-NOT-GROUND",
                $"Surface '{surface.SurfaceId}' has boundary kind '{surface.BoundaryKind}' and cannot be mapped as ground.",
                "GroundBoundaryTopologyMapper.Map"));
        }

        if (metadata.ContactKind == GroundContactKind.Unknown)
        {
            diagnostics.Add(CreateWarning(
                "AE-GROUND-CONTACT-KIND-UNKNOWN",
                $"Surface '{surface.SurfaceId}' has unknown ground contact kind.",
                "GroundBoundaryTopologyMapper.Map"));
        }

        var area = metadata.Geometry.AreaSquareMeters;
        if (!(area > 0.0))
        {
            if (surface.AreaSquareMeters > 0.0)
            {
                area = surface.AreaSquareMeters;
            }
            else
            {
                diagnostics.Add(CreateWarning(
                    "AE-GROUND-SURFACE-AREA-MISSING",
                    $"Surface '{surface.SurfaceId}' requires a positive area from topology or metadata.",
                    "GroundBoundaryTopologyMapper.Map"));
            }
        }

        var floorUValue = metadata.Geometry.FloorUValueWPerSquareMeterKelvin;
        if (RequiresFloorUValue(metadata.ContactKind) && !(floorUValue > 0.0))
        {
            if (surface.UValueWPerSquareMeterKelvin is > 0.0)
            {
                floorUValue = surface.UValueWPerSquareMeterKelvin.Value;
            }
            else
            {
                diagnostics.Add(CreateWarning(
                    "AE-GROUND-SURFACE-UVALUE-MISSING",
                    $"Surface '{surface.SurfaceId}' requires a positive floor U-value from topology or metadata.",
                    "GroundBoundaryTopologyMapper.Map"));
            }
        }

        var wallUValue = metadata.Geometry.WallUValueWPerSquareMeterKelvin;
        if (RequiresWallUValue(metadata.ContactKind) && !(wallUValue > 0.0))
        {
            if (surface.UValueWPerSquareMeterKelvin is > 0.0)
            {
                wallUValue = surface.UValueWPerSquareMeterKelvin.Value;
            }
            else
            {
                diagnostics.Add(CreateWarning(
                    "AE-GROUND-SURFACE-UVALUE-MISSING",
                    $"Surface '{surface.SurfaceId}' requires a positive wall U-value from topology or metadata.",
                    "GroundBoundaryTopologyMapper.Map"));
            }
        }

        var geometry = metadata.Geometry with
        {
            AreaSquareMeters = area,
            FloorUValueWPerSquareMeterKelvin = floorUValue,
            WallUValueWPerSquareMeterKelvin = wallUValue,
            Diagnostics = diagnostics
        };

        return new GroundBoundaryCalculationInput(
            BoundaryId: surface.SurfaceId,
            BuildingId: topology.BuildingId,
            ZoneId: surface.ZoneId,
            RoomId: surface.RoomId,
            SurfaceId: surface.SurfaceId,
            ContactKind: metadata.ContactKind,
            Geometry: geometry,
            Soil: metadata.Soil,
            Climate: metadata.Climate,
            DisclosureOverride: null,
            Source: metadata.Source ?? surface.BoundarySource);
    }

    private static bool RequiresFloorUValue(GroundContactKind kind) =>
        kind is GroundContactKind.SlabOnGround
            or GroundContactKind.SuspendedFloor
            or GroundContactKind.Crawlspace
            or GroundContactKind.HeatedBasement
            or GroundContactKind.UnheatedBasement;

    private static bool RequiresWallUValue(GroundContactKind kind) =>
        kind is GroundContactKind.BuriedWall
            or GroundContactKind.HeatedBasement
            or GroundContactKind.UnheatedBasement;

    private static StandardCalculationDiagnostic CreateWarning(
        string code,
        string message,
        string source) =>
        GroundCalculationDiagnosticsFactory.Create(
            CalculationDiagnosticSeverity.Warning,
            code,
            message,
            StandardCalculationStage.InputPreparation,
            source);
}
