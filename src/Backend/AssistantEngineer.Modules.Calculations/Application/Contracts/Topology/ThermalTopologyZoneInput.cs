namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Topology;

public sealed record ThermalTopologyZoneInput(
    string ZoneId,
    string? Name,
    IReadOnlyList<string> RoomIds);
