using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Ground;

public sealed record GroundSurfaceMetadata(
    string SurfaceId,
    GroundContactKind ContactKind,
    GroundContactGeometry Geometry,
    GroundSoilProperties Soil,
    GroundClimateInput Climate,
    string? Source,
    IReadOnlyList<StandardCalculationDiagnostic> Diagnostics);
