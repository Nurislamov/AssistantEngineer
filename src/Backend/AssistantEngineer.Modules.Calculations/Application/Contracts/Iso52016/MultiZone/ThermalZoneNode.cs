namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.MultiZone;

public sealed record ThermalZoneNode(
    string ZoneId,
    string Name,
    double FloorAreaSquareMeters,
    double VolumeCubicMeters,
    IReadOnlyList<string> BoundaryIds);
