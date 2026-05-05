namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Physical;

/// <summary>
/// Physical surface definition used to expand a room into multiple surface and mass nodes.
/// Optional overrides keep the first implementation deterministic while allowing later calibrated models.
/// </summary>
public sealed record Iso52016PhysicalSurface(
    string SurfaceId,
    Iso52016PhysicalSurfaceBoundaryType BoundaryType,
    double AreaM2,
    IReadOnlyList<Iso52016PhysicalConstructionLayer> ConstructionLayers,
    string? BoundaryId = null,
    string? SurfaceNodeId = null,
    string? MassNodeId = null,
    double? BoundaryConductanceWPerK = null,
    double? SurfaceToAirConductanceWPerK = null,
    double? SurfaceToMassConductanceWPerK = null,
    double? HeatCapacityJPerK = null,
    double? MassHeatCapacityJPerK = null,
    double? SolarGainsDistributionFraction = null,
    double? InternalRadiativeGainsDistributionFraction = null,
    double? AdjacentBoundaryTemperatureC = null);
