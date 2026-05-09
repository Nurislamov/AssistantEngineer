namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Topology;

public sealed record NormalizedThermalBoundary(
    string BoundaryId,
    string SourceZoneId,
    string? AdjacentZoneId,
    BoundaryExposureKind ExposureKind,
    BoundaryElementKind ElementKind,
    double AreaSquareMeters,
    double ConductanceWPerKelvin,
    bool IsTransparent,
    bool IsAdiabaticEquivalent,
    bool RequiresExteriorTemperature,
    bool RequiresGroundTemperature,
    bool RequiresAdjacentZoneTemperature,
    bool RequiresAdjacentUnconditionedTemperature,
    double? OrientationDegrees,
    double? TiltDegrees,
    string? Notes);
