using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Ground;

public sealed record GroundContactGeometry(
    double AreaSquareMeters,
    double? ExposedPerimeterMeters,
    double? CharacteristicDimensionMeters,
    double? DepthBelowGroundMeters,
    double? BasementWallHeightMeters,
    double? CrawlspaceHeightMeters,
    double? FloorUValueWPerSquareMeterKelvin,
    double? WallUValueWPerSquareMeterKelvin,
    double? EdgeInsulationThicknessMeters,
    double? EdgeInsulationConductivityWPerMeterKelvin,
    GroundInsulationPlacement InsulationPlacement,
    IReadOnlyList<StandardCalculationDiagnostic> Diagnostics);
