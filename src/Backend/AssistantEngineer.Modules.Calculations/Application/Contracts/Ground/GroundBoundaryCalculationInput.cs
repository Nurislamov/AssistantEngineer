using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Ground;

public sealed record GroundBoundaryCalculationInput(
    string BoundaryId,
    string? BuildingId,
    string? ZoneId,
    string? RoomId,
    string? SurfaceId,
    GroundContactKind ContactKind,
    GroundContactGeometry Geometry,
    GroundSoilProperties Soil,
    GroundClimateInput Climate,
    StandardCalculationDisclosure? DisclosureOverride,
    string? Source,
    GroundBoundaryType? BoundaryType = null,
    GroundBoundaryCalculationMode CalculationMode = GroundBoundaryCalculationMode.Auto,
    string? ThermalBoundaryId = null,
    string? AdjacentZoneId = null);
