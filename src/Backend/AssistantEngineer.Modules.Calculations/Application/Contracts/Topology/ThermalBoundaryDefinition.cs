namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Topology;

public sealed record ThermalBoundaryDefinition(
    string BoundaryId,
    string SourceZoneId,
    string? AdjacentZoneId,
    BoundaryExposureKind ExposureKind,
    BoundaryElementKind ElementKind,
    double AreaSquareMeters,
    double? UValueWPerSquareMeterKelvin = null,
    double? ConductanceWPerKelvin = null,
    double? HeatCapacityJPerK = null,
    double? InteriorSurfaceResistanceM2KPerW = null,
    double? ExteriorSurfaceResistanceM2KPerW = null,
    double? OrientationDegrees = null,
    double? TiltDegrees = null,
    bool? IsTransparent = null,
    string? Notes = null);
