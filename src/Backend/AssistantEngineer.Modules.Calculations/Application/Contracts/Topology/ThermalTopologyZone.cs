using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Topology;

public sealed record ThermalTopologyZone(
    string ZoneId,
    string? Name,
    IReadOnlyList<string> RoomIds,
    IReadOnlyList<StandardCalculationDiagnostic> Diagnostics);
