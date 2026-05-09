namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.MultiZone;

public sealed record ThermalZoneBoundaryLink(
    string LinkId,
    MultiZoneBoundaryLinkType BoundaryType,
    string SourceZoneId,
    string SourceBoundaryId,
    double AreaSquareMeters,
    double ConductanceWPerK,
    string? TargetZoneId = null,
    AdjacentZoneBoundaryCondition? AdjacentBoundaryCondition = null);
