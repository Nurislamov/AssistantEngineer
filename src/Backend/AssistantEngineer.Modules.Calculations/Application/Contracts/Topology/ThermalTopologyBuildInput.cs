using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Topology;

public sealed record ThermalTopologyBuildInput(
    string BuildingId,
    IReadOnlyList<ThermalTopologyZoneInput> Zones,
    IReadOnlyList<ThermalTopologyRoomInput> Rooms,
    IReadOnlyList<ThermalTopologySurfaceInput> Surfaces,
    StandardCalculationDisclosure? DisclosureOverride);
