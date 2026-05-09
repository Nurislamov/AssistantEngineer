namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Topology;

public sealed record ThermalZoneDefinition(
    string ZoneId,
    string Name,
    ThermalZoneKind Kind,
    double FloorAreaSquareMeters,
    double VolumeCubicMeters,
    string? HeatingSetpointProfileId,
    string? CoolingSetpointProfileId,
    IReadOnlyList<ThermalBoundaryDefinition> Boundaries);
