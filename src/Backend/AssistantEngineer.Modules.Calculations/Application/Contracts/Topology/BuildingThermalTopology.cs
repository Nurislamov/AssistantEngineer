using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Topology;

public sealed record BuildingThermalTopology(
    string BuildingId,
    IReadOnlyList<ThermalTopologyZone> Zones,
    IReadOnlyList<ThermalTopologyRoom> Rooms,
    IReadOnlyList<ThermalTopologySurface> Surfaces,
    StandardCalculationDisclosure Disclosure,
    IReadOnlyList<StandardCalculationDiagnostic> Diagnostics);
